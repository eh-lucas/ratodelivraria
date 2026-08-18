#!/usr/bin/env python3
"""
Sonda de volatilidade de preço das lojas Cedet.

Para quê: descobrir de quanto em quanto tempo as lojas realmente mudam preço, para
decidir com número — e não com palpite — o intervalo entre crawls e a validade do
preço servido do banco.

Como funciona: a cada execução lê algumas páginas do endpoint JSON
`product/search/infiniteScroll` de cada loja (o mesmo que o crawler usa), guarda
`product_id -> preço` num SQLite próprio e compara com a leitura anterior. Só grava
linha quando o preço **muda**; o resto vira contagem. Assim o arquivo cresce com a
informação, não com a repetição.

CARGA NAS LOJAS — a restrição que manda no desenho:
    Não são 67 servidores. 67 domínios resolvem para 2 IPs, então toda requisição que
    abrimos cai na mesma fila, e esse servidor devolve 504 quando pressionado. A sonda
    trata cada IP como um recurso único: uma requisição por vez, pausa entre elas, e
    a pausa **cresce sozinha** quando o servidor começa a responder devagar — a
    latência dele é o pedido de trégua. Medir volatilidade não vale derrubar a loja.

Deliberadamente separado do app: SQLite local, stdlib apenas, nenhuma migration e
nenhum acesso ao Postgres de produção. É instrumento de medida, não peça do produto.

Uso:
    ./volatility.py stores                    # lista as lojas que seriam sondadas
    ./volatility.py snapshot                  # uma leitura
    ./volatility.py snapshot --every 20       # uma leitura a cada 20 min, até Ctrl-C
    ./volatility.py report                    # o que foi medido até agora
"""

from __future__ import annotations

import argparse
import json
import re
import socket
import sqlite3
import sys
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
PROVIDERS_CS = REPO / "Sherlock.Domain" / "Entities" / "Provider.cs"
DB_PATH = HERE / "data" / "volatility.db"

# Mesmo User-Agent e cabeçalhos do CatalogCrawler: a loja responde igual ao que já
# conhecemos, então a medição vale para o crawler de verdade.
HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    ),
    "Accept": "application/json, text/javascript, */*; q=0.01",
    "X-Requested-With": "XMLHttpRequest",
    "Accept-Language": "pt-BR,pt;q=0.9,en-US;q=0.8",
}

PAGE_SIZE = 500        # valor comprovado em produção; 1000 devolve 504

# --- política de carga, por servidor de destino (não por domínio)
BASE_PACE = 8.0        # pausa mínima entre requisições ao mesmo servidor
MAX_PACE = 120.0       # teto da pausa quando o servidor reclama
SLOW_RESPONSE_S = 12.0 # acima disso o servidor está sob pressão: desacelera
FAST_RESPONSE_S = 5.0  # abaixo disso pode voltar devagar ao ritmo base
BACKOFF_HTTP = 120.0   # 429/503: para de mexer nesse servidor por 2 min
TIMEOUT = 120


# --------------------------------------------------------------------------- lojas

def load_stores(only_active: bool = True) -> list[dict]:
    """Lê as lojas do próprio Provider.cs — evita uma segunda lista para manter em dia."""
    text = PROVIDERS_CS.read_text(encoding="utf-8-sig")

    stores = []
    for block in re.split(r"public static Provider ", text)[1:]:
        ident = block.split("=", 1)[0].strip()
        pid = re.search(r"\bId\s*=\s*(\d+)", block)
        name = re.search(r'\bName\s*=\s*"([^"]+)"', block)
        url = re.search(r'\bUrl\s*=\s*"([^"]+)"', block)
        if not (pid and url):
            continue
        active = re.search(r"\bIsActive\s*=\s*false", block) is None
        if only_active and not active:
            continue
        stores.append({
            "id": int(pid.group(1)),
            "name": name.group(1) if name else ident,
            "url": url.group(1).rstrip("/"),
            "host": urllib.parse.urlsplit(url.group(1)).hostname or "",
        })

    stores.sort(key=lambda s: s["id"])
    return stores


def resolve_ip(host: str) -> str:
    try:
        return socket.gethostbyname(host)
    except OSError:
        return "?"


# ------------------------------------------------------------------- carga/ritmo

class Server:
    """Um servidor de destino. Serializa e ritma tudo que vai para aquele IP.

    A pausa é adaptativa porque não temos como saber de fora quanta carga o servidor
    aguenta — mas ele nos diz: quando começa a demorar, é porque está na fila. Então
    resposta lenta aumenta a pausa e resposta rápida devolve o ritmo aos poucos.
    """

    def __init__(self, ip: str, base_pace: float = BASE_PACE):
        self.ip = ip
        self.base_pace = base_pace
        self.pace = base_pace
        self.last_request = 0.0
        self.requests = 0
        self.slow = 0
        self.errors = 0
        self._lock = threading.Lock()

    def wait(self) -> None:
        with self._lock:
            elapsed = time.monotonic() - self.last_request
            if self.last_request and elapsed < self.pace:
                time.sleep(self.pace - elapsed)
            self.last_request = time.monotonic()

    def observe(self, seconds: float, status: int | None = None,
                failed: bool = False) -> None:
        self.requests += 1
        if status in (429, 503) or failed:
            self.errors += 1
            self.pace = min(MAX_PACE, max(self.pace * 2, BACKOFF_HTTP))
        elif seconds > SLOW_RESPONSE_S:
            self.slow += 1
            self.pace = min(MAX_PACE, self.pace * 1.5)
        elif seconds < FAST_RESPONSE_S:
            self.pace = max(self.base_pace, self.pace * 0.8)

    def summary(self) -> str:
        return (f"{self.ip}: {self.requests} req, ritmo {self.pace:.0f}s"
                + (f", {self.slow} lentas" if self.slow else "")
                + (f", {self.errors} erros" if self.errors else ""))


# ------------------------------------------------------------------------- fetch

def parse_price(raw) -> float | None:
    """Aceita "R$ 1.234,56", "91,18", número cru. Ponto é milhar, vírgula é decimal."""
    if raw is None or raw is False or raw == "":
        return None
    if isinstance(raw, (int, float)):
        return float(raw)
    digits = re.sub(r"[^\d,.]", "", str(raw))
    if not digits:
        return None
    digits = digits.replace(".", "").replace(",", ".")
    try:
        value = float(digits)
    except ValueError:
        return None
    return value if value > 0 else None


def fetch_page(base_url: str, page: int) -> tuple[list[dict], float]:
    url = (f"{base_url}/index.php?route=product/search/infiniteScroll"
           f"&search=&page={page}&limit={PAGE_SIZE}&sort=p.date_added&order=DESC")
    req = urllib.request.Request(url, headers=HEADERS)

    started = time.monotonic()
    with urllib.request.urlopen(req, timeout=TIMEOUT) as resp:
        payload = json.loads(resp.read().decode("utf-8", "replace"))
    elapsed = time.monotonic() - started

    out = []
    for p in payload.get("products") or []:
        product_id = str(p.get("product_id") or "").strip()
        name = (p.get("name") or "").strip()
        if not product_id or not name:
            continue
        listed = parse_price(p.get("price"))
        special = parse_price(p.get("special"))
        effective = special if special is not None else listed
        if effective is None:
            continue
        out.append({
            "product_id": product_id,
            "name": name[:300],
            "listed": listed,
            "special": special,
            "effective": effective,
        })
    return out, elapsed


def read_store(store: dict, pages: int, server: Server):
    """Lê as `pages` primeiras páginas da loja, no ritmo que o servidor permitir.

    Devolve também as latências: é o número que diz se a varredura completa cabe numa
    janela de madrugada ou precisa de duas noites.
    """
    products: dict[str, dict] = {}
    fetched = 0
    timings: list[float] = []

    for page in range(1, pages + 1):
        server.wait()
        try:
            items, elapsed = fetch_page(store["url"], page)
            server.observe(elapsed)
            timings.append(elapsed)
        except urllib.error.HTTPError as exc:
            server.observe(0, status=exc.code, failed=True)
            return products, fetched, f"page {page}: HTTP {exc.code}", timings
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError,
                socket.timeout, OSError) as exc:
            server.observe(0, failed=True)
            return products, fetched, f"page {page}: {type(exc).__name__}: {exc}", timings

        fetched += 1
        if not items:
            break

        novel = 0
        for item in items:
            if item["product_id"] not in products:
                products[item["product_id"]] = item
                novel += 1
        # Loja que ignora o parâmetro de página devolveria sempre o mesmo bloco.
        if novel == 0:
            break

    return products, fetched, None, timings


# ---------------------------------------------------------------------------- db

SCHEMA = """
CREATE TABLE IF NOT EXISTS current (
    store_id     INTEGER NOT NULL,
    product_id   TEXT    NOT NULL,
    name         TEXT,
    listed       REAL,
    special      REAL,
    effective    REAL    NOT NULL,
    first_seen   TEXT    NOT NULL,
    last_seen    TEXT    NOT NULL,
    last_changed TEXT,
    PRIMARY KEY (store_id, product_id)
);

-- Uma linha por mudança de preço observada. É o dado que responde a pergunta.
CREATE TABLE IF NOT EXISTS changes (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    store_id      INTEGER NOT NULL,
    store_name    TEXT,
    product_id    TEXT    NOT NULL,
    name          TEXT,
    detected_at   TEXT    NOT NULL,
    prev_seen_at  TEXT    NOT NULL,
    gap_seconds   INTEGER NOT NULL,
    prev_effective REAL   NOT NULL,
    new_effective  REAL   NOT NULL,
    prev_special   REAL,
    new_special    REAL,
    kind          TEXT    NOT NULL  -- promo_in | promo_out | promo_change | list_change
);
CREATE INDEX IF NOT EXISTS ix_changes_detected ON changes (detected_at);

-- Uma linha por loja por execução: o denominador das taxas.
CREATE TABLE IF NOT EXISTS runs (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    store_id     INTEGER NOT NULL,
    store_name   TEXT,
    started_at   TEXT    NOT NULL,
    finished_at  TEXT    NOT NULL,
    pages        INTEGER NOT NULL,
    products     INTEGER NOT NULL,
    paired       INTEGER NOT NULL,  -- produtos que já tinham leitura anterior
    changed      INTEGER NOT NULL,
    median_gap_s INTEGER,
    median_response_ms INTEGER,   -- decide se a varredura cabe numa madrugada
    error        TEXT
);
"""


def open_db() -> sqlite3.Connection:
    DB_PATH.parent.mkdir(parents=True, exist_ok=True)
    # Uma thread por servidor de destino; a escrita é serializada por um lock no
    # chamador, então a conexão pode ser compartilhada.
    conn = sqlite3.connect(DB_PATH, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    conn.executescript(SCHEMA)

    # Banco criado antes desta coluna existir.
    columns = {r["name"] for r in conn.execute("PRAGMA table_info(runs)")}
    if "median_response_ms" not in columns:
        conn.execute("ALTER TABLE runs ADD COLUMN median_response_ms INTEGER")
        conn.commit()

    return conn


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def parse_iso(value: str) -> datetime:
    return datetime.fromisoformat(value)


def classify(prev_special, new_special) -> str:
    """Distingue promoção entrando/saindo de mudança de preço de tabela.

    Importa para a decisão: se quase toda mudança é promoção, o intervalo de crawl
    deve seguir o ritmo das campanhas, não o do cadastro.
    """
    if prev_special is None and new_special is not None:
        return "promo_in"
    if prev_special is not None and new_special is None:
        return "promo_out"
    if prev_special is not None and new_special is not None:
        return "promo_change"
    return "list_change"


# --------------------------------------------------------------------- snapshot

def persist_store(conn: sqlite3.Connection, store: dict, products: dict,
                  fetched: int, error: str | None, started: str,
                  timings: list[float] | None = None) -> dict:
    """Grava a leitura. Chamado com o lock: a rede já aconteceu fora dele."""
    finished = now_iso()
    finished_dt = parse_iso(finished)

    previous = {
        row["product_id"]: row
        for row in conn.execute(
            "SELECT product_id, listed, special, effective, last_seen FROM current "
            "WHERE store_id = ?", (store["id"],))
    }

    paired = 0
    changed = 0
    gaps: list[int] = []

    for product_id, item in products.items():
        prev = previous.get(product_id)
        if prev is None:
            conn.execute(
                "INSERT INTO current (store_id, product_id, name, listed, special, "
                "effective, first_seen, last_seen) VALUES (?,?,?,?,?,?,?,?)",
                (store["id"], product_id, item["name"], item["listed"],
                 item["special"], item["effective"], finished, finished))
            continue

        paired += 1
        gap = int((finished_dt - parse_iso(prev["last_seen"])).total_seconds())
        gaps.append(gap)

        if abs(item["effective"] - prev["effective"]) < 0.005:
            conn.execute(
                "UPDATE current SET last_seen = ?, name = ? "
                "WHERE store_id = ? AND product_id = ?",
                (finished, item["name"], store["id"], product_id))
            continue

        changed += 1
        conn.execute(
            "INSERT INTO changes (store_id, store_name, product_id, name, detected_at, "
            "prev_seen_at, gap_seconds, prev_effective, new_effective, prev_special, "
            "new_special, kind) VALUES (?,?,?,?,?,?,?,?,?,?,?,?)",
            (store["id"], store["name"], product_id, item["name"], finished,
             prev["last_seen"], gap, prev["effective"], item["effective"],
             prev["special"], item["special"],
             classify(prev["special"], item["special"])))
        conn.execute(
            "UPDATE current SET listed = ?, special = ?, effective = ?, last_seen = ?, "
            "last_changed = ?, name = ? WHERE store_id = ? AND product_id = ?",
            (item["listed"], item["special"], item["effective"], finished, finished,
             item["name"], store["id"], product_id))

    median_gap = sorted(gaps)[len(gaps) // 2] if gaps else None
    ordered = sorted(timings or [])
    median_ms = int(1000 * ordered[len(ordered) // 2]) if ordered else None
    conn.execute(
        "INSERT INTO runs (store_id, store_name, started_at, finished_at, pages, "
        "products, paired, changed, median_gap_s, median_response_ms, error) "
        "VALUES (?,?,?,?,?,?,?,?,?,?,?)",
        (store["id"], store["name"], started, finished, fetched, len(products),
         paired, changed, median_gap, median_ms, error))
    conn.commit()

    return {"store": store, "products": len(products), "paired": paired,
            "changed": changed, "gap": median_gap, "error": error,
            "latency_ms": median_ms}


def snapshot(stores: list[dict], pages: int, base_pace: float,
             quiet: bool = False) -> None:
    """Uma leitura de todas as lojas, uma requisição por vez em cada servidor."""
    by_ip: dict[str, list[dict]] = defaultdict(list)
    for store in stores:
        by_ip[resolve_ip(store["host"])].append(store)

    servers = {ip: Server(ip, base_pace) for ip in by_ip}
    conn = open_db()
    lock = threading.Lock()
    results: list[dict] = []

    def worker(ip: str, group: list[dict]) -> None:
        server = servers[ip]
        for store in group:
            try:
                # A rede fica fora do lock do banco: uma loja lenta não impede a
                # outra thread de gravar o que já leu.
                started = now_iso()
                products, fetched, error, timings = read_store(store, pages, server)
                with lock:
                    outcome = persist_store(conn, store, products, fetched,
                                            error, started, timings)
            except Exception as exc:  # noqa: BLE001 - uma loja não derruba a rodada
                outcome = {"store": store, "products": 0, "paired": 0, "changed": 0,
                           "gap": None, "latency_ms": None,
                           "error": f"{type(exc).__name__}: {exc}"}
            results.append(outcome)
            if not quiet:
                gap = f"{outcome['gap'] / 60:.0f}min" if outcome["gap"] else "-"
                flag = f"  ERRO {outcome['error']}" if outcome["error"] else ""
                lat = (f"{outcome['latency_ms'] / 1000:.0f}s"
                       if outcome.get("latency_ms") else "-")
                print(f"  {store['name'][:34]:<34} {outcome['products']:>5} itens  "
                      f"{outcome['paired']:>5} pareados  {outcome['changed']:>3} mudaram  "
                      f"gap {gap}  resposta {lat}{flag}", flush=True)

    threads = [threading.Thread(target=worker, args=(ip, group), daemon=True)
               for ip, group in by_ip.items()]
    for thread in threads:
        thread.start()
    for thread in threads:
        thread.join()

    paired = sum(r["paired"] for r in results)
    changed = sum(r["changed"] for r in results)
    pct = f"{100 * changed / paired:.3f}%" if paired else "-"
    print(f"{now_iso()}  {len(results)} lojas  {paired} pareados  "
          f"{changed} mudaram ({pct})")
    for server in servers.values():
        print(f"  servidor {server.summary()}")
    conn.close()


# ----------------------------------------------------------------------- report

GAP_BUCKETS = [
    ("< 30min", 0, 1800),
    ("30min-1h", 1800, 3600),
    ("1-2h", 3600, 7200),
    ("2-6h", 7200, 21600),
    ("6-12h", 21600, 43200),
    ("12-24h", 43200, 86400),
    ("> 24h", 86400, 10 ** 9),
]


def report(_args=None) -> None:
    conn = open_db()

    runs = conn.execute(
        "SELECT COUNT(*) n, COUNT(DISTINCT store_id) lojas, MIN(started_at) ini, "
        "MAX(finished_at) fim, SUM(paired) paired, SUM(changed) changed, "
        "SUM(products) produtos, SUM(error IS NOT NULL) erros FROM runs").fetchone()

    if not runs["n"]:
        print("Nenhuma leitura ainda. Rode: ./volatility.py snapshot")
        return

    tracked = conn.execute("SELECT COUNT(*) n FROM current").fetchone()["n"]
    print("=" * 74)
    print("VOLATILIDADE DE PREÇO — o que foi medido")
    print("=" * 74)
    print(f"Janela            {runs['ini']}  ->  {runs['fim']}")
    print(f"Leituras          {runs['n']} (loja x execução), {runs['lojas']} lojas, "
          f"{runs['erros']} com erro")
    print(f"Produtos seguidos {tracked}")
    print(f"Pareados          {runs['paired'] or 0}  "
          f"(leituras com preço anterior para comparar)")

    paired = runs["paired"] or 0
    changed = runs["changed"] or 0
    if not paired:
        print("\nAinda sem par para comparar — rode um segundo snapshot mais tarde.")
        conn.close()
        return

    print(f"Mudaram           {changed}  ({100 * changed / paired:.3f}% das leituras)")

    # --- por intervalo entre leituras: é a curva que define o TTL
    print("\n" + "-" * 74)
    print("TAXA DE MUDANÇA POR INTERVALO ENTRE LEITURAS")
    print("-" * 74)
    print(f"{'intervalo':<12}{'pareados':>10}{'mudaram':>9}{'taxa':>9}")

    run_rows = conn.execute(
        "SELECT median_gap_s, paired, changed FROM runs "
        "WHERE paired > 0 AND median_gap_s IS NOT NULL").fetchall()

    safe_ttl = None
    for label, low, high in GAP_BUCKETS:
        rows = [r for r in run_rows if low <= r["median_gap_s"] < high]
        if not rows:
            continue
        b_paired = sum(r["paired"] for r in rows)
        b_changed = sum(r["changed"] for r in rows)
        rate = 100 * b_changed / b_paired if b_paired else 0
        print(f"{label:<12}{b_paired:>10}{b_changed:>9}{rate:>8.3f}%")
        if b_changed == 0 and b_paired >= 200:
            safe_ttl = label

    # --- hora do dia: mostra se a loja mexe em preço em lote, num horário
    print("\n" + "-" * 74)
    print("MUDANÇAS POR HORA DO DIA (UTC, hora em que foram detectadas)")
    print("-" * 74)
    hours = conn.execute(
        "SELECT CAST(strftime('%H', detected_at) AS INTEGER) h, COUNT(*) n "
        "FROM changes GROUP BY h ORDER BY h").fetchall()
    if hours:
        peak = max(r["n"] for r in hours)
        for row in hours:
            bar = "#" * max(1, round(20 * row["n"] / peak))
            print(f"  {row['h']:02d}h  {row['n']:>4}  {bar}")
    else:
        print("  (nenhuma mudança detectada ainda)")

    # --- latência por hora: decide se a varredura completa cabe numa madrugada
    lat = conn.execute(
        "SELECT CAST(strftime('%H', finished_at) AS INTEGER) h, COUNT(*) n, "
        "AVG(median_response_ms) ms FROM runs "
        "WHERE median_response_ms IS NOT NULL GROUP BY h ORDER BY h").fetchall()
    if lat:
        print("\n" + "-" * 74)
        print("LATÊNCIA POR HORA DO DIA (UTC) — quanto a varredura completa avança")
        print("-" * 74)
        print("  Uma passada completa são 2.479 requisições (67 lojas x 37 páginas),")
        print("  ~1.240 por servidor. Numa janela de 6h cabem 21.600s por servidor.")
        for row in lat:
            seconds = row["ms"] / 1000
            # O ritmo real é o maior entre a resposta e a pausa mínima entre requisições.
            pace = max(seconds, BASE_PACE)
            fits = 21600 / pace
            verdict = "cabe numa noite" if fits >= 1240 else f"{100 * fits / 1240:.0f}% de uma passada"
            print(f"  {row['h']:02d}h  resposta ~{seconds:5.1f}s  "
                  f"({row['n']:>3} leituras)  ->  {verdict}")

    # --- natureza e tamanho da mudança
    kinds = conn.execute(
        "SELECT kind, COUNT(*) n FROM changes GROUP BY kind ORDER BY n DESC").fetchall()
    if kinds:
        print("\n" + "-" * 74)
        print("NATUREZA DA MUDANÇA")
        print("-" * 74)
        labels = {"promo_in": "promoção entrou", "promo_out": "promoção saiu",
                  "promo_change": "promoção mudou de valor",
                  "list_change": "preço de tabela mudou"}
        for row in kinds:
            print(f"  {labels.get(row['kind'], row['kind']):<26}{row['n']:>6}"
                  f"{100 * row['n'] / changed:>8.1f}%")

        deltas = [abs(100 * (r["new_effective"] - r["prev_effective"]) / r["prev_effective"])
                  for r in conn.execute(
                      "SELECT prev_effective, new_effective FROM changes "
                      "WHERE prev_effective > 0")]
        deltas.sort()
        if deltas:
            print(f"\n  Tamanho da mudança (|%|):  mediana {deltas[len(deltas)//2]:.1f}%   "
                  f"p90 {deltas[int(0.9 * (len(deltas) - 1))]:.1f}%   máx {deltas[-1]:.1f}%")

    # --- lojas que mais mexem
    stores = conn.execute(
        "SELECT store_name, SUM(paired) paired, SUM(changed) changed FROM runs "
        "GROUP BY store_id HAVING paired > 0 AND changed > 0 "
        "ORDER BY 1.0 * SUM(changed) / SUM(paired) DESC LIMIT 10").fetchall()
    if stores:
        print("\n" + "-" * 74)
        print("LOJAS QUE MAIS MUDAM PREÇO")
        print("-" * 74)
        for row in stores:
            print(f"  {row['store_name'][:40]:<40}{row['changed']:>5}/{row['paired']:<7}"
                  f"{100 * row['changed'] / row['paired']:>7.3f}%")

    # --- leitura prática
    print("\n" + "-" * 74)
    print("LEITURA")
    print("-" * 74)
    span_h = max(
        0.01,
        (parse_iso(runs["fim"]) - parse_iso(runs["ini"])).total_seconds() / 3600)
    per_day = 100 * changed / paired * (24 / span_h) if span_h else 0
    print(f"  Taxa observada por produto/dia: ~{per_day:.2f}%  "
          f"(extrapolado de {span_h:.1f}h de janela)")
    if safe_ttl:
        print(f"  Nenhuma mudança em intervalos de {safe_ttl} com amostra suficiente:")
        print(f"  servir preço do banco com até {safe_ttl} de idade não muda o resultado.")
    if span_h < 24:
        print("  ⚠ Janela menor que 24h: ainda não cobre o ciclo diário das lojas.")
    conn.close()


# ------------------------------------------------------------------------- main

def cmd_stores(args) -> None:
    stores = load_stores(only_active=not args.all)
    by_ip: dict[str, list[dict]] = defaultdict(list)
    for store in stores:
        by_ip[resolve_ip(store["host"])].append(store)
    print(f"{len(stores)} lojas em {len(by_ip)} servidores\n")
    for ip, group in sorted(by_ip.items(), key=lambda kv: -len(kv[1])):
        print(f"{ip}  ({len(group)} lojas)")
        for store in group:
            print(f"    {store['id']:>3}  {store['name'][:44]:<44} {store['host']}")


def describe_load(stores: list[dict], pages: int, pace: float,
                  every: int | None) -> str:
    """Deixa a carga à vista antes de gerar qualquer uma: é a restrição do projeto."""
    by_ip: dict[str, int] = defaultdict(int)
    for store in stores:
        by_ip[resolve_ip(store["host"])] += 1
    if not by_ip:
        return "nenhuma loja selecionada"

    worst = max(by_ip.values())
    reqs = worst * pages
    round_s = reqs * pace
    line = (f"carga: {len(stores)} lojas / {len(by_ip)} servidores, "
            f"{sum(by_ip.values()) * pages} requisições por rodada "
            f"({reqs} no servidor mais carregado, >= {pace:.0f}s entre elas "
            f"= {round_s / 60:.1f}min)")
    if every:
        per_hour = reqs * (60 / every)
        line += (f"\n       a cada {every}min = {per_hour:.0f} req/hora no servidor "
                 f"mais carregado (1 a cada {60 / max(per_hour, 0.01):.1f}min)")
        if round_s > every * 60:
            line += "\n       ⚠ a rodada não cabe no intervalo — aumente --every"
    return line


def cmd_snapshot(args) -> None:
    stores = load_stores(only_active=not args.all)
    if args.stores:
        wanted = {int(x) for x in args.stores.split(",")}
        stores = [s for s in stores if s["id"] in wanted]
    elif args.limit:
        stores = stores[:args.limit]

    print(describe_load(stores, args.pages, args.pace, args.every), flush=True)

    round_no = 0
    while True:
        round_no += 1
        print(f"\n=== leitura {round_no} — {now_iso()} — {len(stores)} lojas, "
              f"{args.pages} página(s) de {PAGE_SIZE} ===", flush=True)
        snapshot(stores, args.pages, args.pace, quiet=args.quiet)
        if not args.every:
            return
        print(f"próxima leitura em {args.every}min (Ctrl-C para parar)", flush=True)
        time.sleep(args.every * 60)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Mede de quanto em quanto tempo as lojas mudam preço.")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_stores = sub.add_parser("stores", help="lista as lojas e os servidores")
    p_stores.add_argument("--all", action="store_true", help="inclui as inativas")
    p_stores.set_defaults(func=cmd_stores)

    p_snap = sub.add_parser("snapshot", help="faz uma leitura de preços")
    p_snap.add_argument("--pages", type=int, default=1,
                        help=f"páginas de {PAGE_SIZE} por loja (default 1)")
    p_snap.add_argument("--limit", type=int, default=6,
                        help="usa só as N primeiras lojas (default 6; 0 = todas)")
    p_snap.add_argument("--stores", help="ids específicos, separados por vírgula")
    p_snap.add_argument("--every", type=int, help="repete a cada N minutos")
    p_snap.add_argument("--pace", type=float, default=BASE_PACE,
                        help=f"segundos mínimos entre requisições ao mesmo servidor "
                             f"(default {BASE_PACE:.0f}; cresce sozinho se o servidor "
                             f"ficar lento)")
    p_snap.add_argument("--all", action="store_true", help="inclui lojas inativas")
    p_snap.add_argument("--quiet", action="store_true", help="só o resumo")
    p_snap.set_defaults(func=cmd_snapshot)

    p_rep = sub.add_parser("report", help="mostra o que já foi medido")
    p_rep.set_defaults(func=report)

    args = parser.parse_args()
    args.func(args)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("\ninterrompido")
        sys.exit(130)

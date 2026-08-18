/**
 * Gerador de BR Code (Pix "copia e cola") no padrão EMV-MPM do Banco Central.
 *
 * O código é uma sequência de campos TLV — dois dígitos de ID, dois de tamanho,
 * e o valor — fechada por um CRC16 que os apps de banco conferem antes de aceitar.
 * Tudo é montado aqui no navegador: não há chamada de rede nem intermediário.
 */

export interface PixParams {
  /** Chave Pix: telefone em formato E.164 (+5546988267525), e-mail, CPF ou EVP. */
  key: string;
  /** Nome exibido no QR, até 25 caracteres. Campo informativo. */
  name: string;
  /** Cidade do recebedor, até 15 caracteres. */
  city: string;
  /** Valor em reais. Se omitido, quem paga digita o valor. */
  amount?: number;
}

/** Monta um campo TLV: id + tamanho com 2 dígitos + valor. */
function field(id: string, value: string): string {
  return id + String(value.length).padStart(2, '0') + value;
}

/** Remove acentos e caracteres fora do ASCII, que o padrão não aceita. */
function sanitize(text: string, maxLength: number): string {
  return text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^\x20-\x7E]/g, '')
    .trim()
    .slice(0, maxLength);
}

/** CRC16/CCITT-FALSE — polinômio 0x1021, valor inicial 0xFFFF. */
function crc16(payload: string): string {
  let crc = 0xffff;
  for (let i = 0; i < payload.length; i++) {
    crc ^= payload.charCodeAt(i) << 8;
    for (let bit = 0; bit < 8; bit++) {
      crc = crc & 0x8000 ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff;
    }
  }
  return crc.toString(16).toUpperCase().padStart(4, '0');
}

export function buildPixPayload({ key, name, city, amount }: PixParams): string {
  const merchantAccount =
    field('00', 'br.gov.bcb.pix') +
    field('01', key);

  let payload =
    field('00', '01') +                       // versão do formato
    field('26', merchantAccount) +            // dados da conta Pix
    field('52', '0000') +                     // categoria do estabelecimento
    field('53', '986') +                      // moeda: BRL
    (amount && amount > 0 ? field('54', amount.toFixed(2)) : '') +
    field('58', 'BR') +                       // país
    field('59', sanitize(name, 25)) +
    field('60', sanitize(city, 15)) +
    field('62', field('05', '***'));          // sem identificador de transação

  payload += '6304';                          // o CRC entra no próprio cálculo
  return payload + crc16(payload);
}

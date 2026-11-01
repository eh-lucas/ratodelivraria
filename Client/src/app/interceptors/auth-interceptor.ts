import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth-service';

/**
 * Anexa o Bearer token e, quando ele vence, renova a sessão sem a pessoa
 * perceber.
 *
 * O refresh é o que faz valer a pena ter token de acesso curto: sem ele, uma
 * sessão de 30 minutos derrubaria a pessoa no meio de uma busca, e quem entrou
 * pelo Google teria que abrir o e-mail de novo. Com ele, quem usa o site nunca é
 * deslogado (docs/PLANO-OBSERVADOS.md §8.1).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  const comToken = token && !req.headers.has('Authorization')
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(comToken).pipe(
    catchError((erro: unknown) => {
      if (!(erro instanceof HttpErrorResponse) || erro.status !== 401) {
        return throwError(() => erro);
      }

      // O próprio refresh voltando 401 significa que a sessão longa acabou —
      // tentar renovar de novo seria um laço infinito.
      if (req.url.includes('/auth/refresh') || req.url.includes('/auth/login')) {
        return throwError(() => erro);
      }

      // Sem cookie de sessão não há o que renovar: é um visitante batendo numa
      // rota que exige conta, e o 401 é a resposta certa.
      if (!auth.temSessaoLonga()) {
        return throwError(() => erro);
      }

      return renovarERepetir(req, next, auth, erro);
    }),
  );
};

/**
 * Uma renovação por vez. Sem isto, uma tela que dispara três chamadas ao abrir
 * faria três refreshes — e como o token roda a cada uso, dois deles chegariam
 * com um valor já gasto, o que o backend trata (corretamente) como sinal de
 * roubo e derruba a sessão inteira.
 */
function renovarERepetir(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  auth: AuthService,
  erroOriginal: HttpErrorResponse,
): Observable<HttpEvent<unknown>> {
  if (auth.renovando) {
    return auth.tokenRenovado$.pipe(
      filter((t): t is string => t !== null),
      take(1),
      switchMap(novo => next(req.clone({ setHeaders: { Authorization: `Bearer ${novo}` } }))),
    );
  }

  return auth.refresh().pipe(
    switchMap(novo => next(req.clone({ setHeaders: { Authorization: `Bearer ${novo}` } }))),
    catchError(() => {
      auth.encerrarSessaoLocal();
      return throwError(() => erroOriginal);
    }),
  );
}

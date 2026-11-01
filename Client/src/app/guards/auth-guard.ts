import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth-service';
import { inject } from '@angular/core';

/**
 * Só para as telas que não têm versão deslogada (perfil, créditos).
 *
 * Observados e histórico NÃO usam este guard: lá a página abre e convida a
 * entrar — ver o comentário em app.routes.ts.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Guarda para onde a pessoa queria ir, para voltar lá depois de entrar.
  router.navigate(['/login'], { queryParams: { redirect: state.url } });
  return false;
};

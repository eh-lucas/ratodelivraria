import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

interface GoogleConfig {
  enabled: boolean;
  clientId: string;
}

/**
 * Carrega o Google Identity Services e desenha o botão.
 *
 * O client id vem do backend (`/auth/google/config`) em vez de ficar no
 * environment.ts: assim o mesmo build serve dev e produção, que usam client ids
 * diferentes, e ligar ou desligar o botão é uma variável de ambiente — não um
 * rebuild do front.
 */
@Injectable({ providedIn: 'root' })
export class GoogleIdentityService {
  private static readonly SCRIPT_URL = 'https://accounts.google.com/gsi/client';

  private config?: GoogleConfig;
  private scriptPromise?: Promise<void>;

  constructor(private http: HttpClient) {}

  async getConfig(): Promise<GoogleConfig> {
    if (!this.config) {
      try {
        this.config = await firstValueFrom(
          this.http.get<GoogleConfig>(`${environment.apiUrl}/auth/google/config`),
        );
      } catch {
        // API fora do ar não pode levar a tela de login junto: sem o botão do
        // Google, o formulário de e-mail e senha continua ali.
        this.config = { enabled: false, clientId: '' };
      }
    }
    return this.config;
  }

  /**
   * Desenha o botão dentro do elemento dado. Devolve false quando não há client id
   * configurado ou o script não carregou — quem chama esconde o bloco.
   */
  async renderButton(host: HTMLElement, onCredential: (credential: string) => void): Promise<boolean> {
    const config = await this.getConfig();
    if (!config.enabled) return false;

    try {
      await this.loadScript();
    } catch {
      return false;
    }

    const google = (window as any).google;
    if (!google?.accounts?.id) return false;

    google.accounts.id.initialize({
      client_id: config.clientId,
      callback: (response: { credential: string }) => onCredential(response.credential),
    });

    google.accounts.id.renderButton(host, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      locale: 'pt-BR',
      width: host.clientWidth || 320,
    });

    return true;
  }

  private loadScript(): Promise<void> {
    // Uma vez só: a tela de login pode ser montada de novo sem recarregar a página.
    if (this.scriptPromise) return this.scriptPromise;

    this.scriptPromise = new Promise<void>((resolve, reject) => {
      if ((window as any).google?.accounts?.id) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = GoogleIdentityService.SCRIPT_URL;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Falha ao carregar o Google Identity Services'));
      document.head.appendChild(script);
    });

    return this.scriptPromise;
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { UserService, CreditPackage, UserCredits } from '../../services/user-service';

@Component({
  selector: 'app-credits-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './credits-page.html',
  styleUrl: './credits-page.scss'
})
export class CreditsPage implements OnInit {
  packages: CreditPackage[] = [];
  userCredits: UserCredits | null = null;
  loading = true;
  purchasing = false;
  error: string | null = null;
  successMessage: string | null = null;
  selectedPackageId: number | null = null;

  constructor(
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = null;

    // Carrega pacotes e créditos do usuário em paralelo
    this.userService.getCreditPackages().subscribe({
      next: (packages) => {
        this.packages = packages;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar pacotes:', err);
        this.error = 'Nao foi possivel carregar os pacotes de creditos.';
        this.loading = false;
      }
    });

    this.userService.getCurrentUser().subscribe({
      next: (credits) => {
        this.userCredits = credits;
      },
      error: (err) => {
        console.error('Erro ao carregar creditos:', err);
      }
    });
  }

  selectPackage(packageId: number): void {
    this.selectedPackageId = packageId;
    this.error = null;
    this.successMessage = null;
  }

  purchasePackage(pkg: CreditPackage): void {
    if (this.purchasing) return;

    this.purchasing = true;
    this.error = null;
    this.successMessage = null;
    this.selectedPackageId = pkg.id;

    // Simula um paymentId - em producao, isso viria de um gateway de pagamento
    const mockPaymentId = `PAY-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    this.userService.purchaseCredits({ packageId: pkg.id, paymentId: mockPaymentId }).subscribe({
      next: (result) => {
        if (result.success) {
          this.successMessage = `Compra realizada com sucesso! +${result.amount} creditos adicionados.`;
          // Atualiza o saldo local
          if (this.userCredits) {
            this.userCredits.availableCredits = result.newBalance;
          }
        } else {
          this.error = result.message || 'Erro ao processar compra.';
        }
        this.purchasing = false;
        this.selectedPackageId = null;
      },
      error: (err) => {
        console.error('Erro ao comprar pacote:', err);
        this.error = 'Erro ao processar a compra. Tente novamente.';
        this.purchasing = false;
        this.selectedPackageId = null;
      }
    });
  }

  getPackageIcon(packageName: string): string {
    const name = packageName.toLowerCase();
    if (name.includes('starter') || name.includes('inicial')) return 'rocket';
    if (name.includes('basic') || name.includes('basico')) return 'star';
    if (name.includes('popular')) return 'fire';
    if (name.includes('premium') || name.includes('pro')) return 'crown';
    return 'package';
  }

  getPackageColor(packageName: string): string {
    const name = packageName.toLowerCase();
    if (name.includes('starter') || name.includes('inicial')) return 'blue';
    if (name.includes('basic') || name.includes('basico')) return 'green';
    if (name.includes('popular')) return 'purple';
    if (name.includes('premium') || name.includes('pro')) return 'yellow';
    return 'gray';
  }
}

export const environment = {
  production: true,
  apiUrl: '/api',
  appName: 'ratodelivraria',
  // Doação via Pix estático: o QR é gerado no navegador, sem intermediário.
  pix: {
    key: '+5546988267525',
    name: 'RATODELIVRARIA',
    city: 'CURITIBA',
  },
  // Branch de demonstração: acesso aberto, sem login (backend usa usuário master).
  demoMode: true,
};
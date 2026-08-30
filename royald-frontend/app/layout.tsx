import type { Metadata } from 'next';
import Providers from '../components/Providers';
import ClientEffects from '../components/ClientEffects';

export const metadata: Metadata = {
  title: 'Royal-D Sales & Debtor Management System',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="th">
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link
          rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"
        />
        <link
          rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css"
        />
      </head>
      <body style={{ backgroundColor: '#f8fafc', minHeight: '100vh' }}>
        <Providers>
          <ClientEffects />
          {children}
        </Providers>
      </body>
    </html>
  );
}

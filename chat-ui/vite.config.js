import { defineConfig } from 'vite';
export default defineConfig(({ mode }) => ({
  server: { port: 5173, strictPort: true, proxy: { '/api': 'http://localhost:5080' } },
  build: { outDir: mode === 'dotnet' ? '../ChatApi/wwwroot' : 'dist', emptyOutDir: true }
}));

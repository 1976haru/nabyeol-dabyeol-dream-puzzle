import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// 상대 경로 base 로 GitHub Pages(하위 경로)와 Capacitor(file://) 양쪽 모두 지원
// https://vite.dev/config/
export default defineConfig({
  base: './',
  plugins: [react()],
  build: {
    outDir: 'dist',
  },
});

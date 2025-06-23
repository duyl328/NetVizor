// vite-plugins/theme-inject.ts
import type { Plugin } from 'vite'

export function themeInjectPlugin(): Plugin {
  return {
    name: 'theme-inject',
    transformIndexHtml(html) {
      const themeScript = `
        <script>
          (function() {
            const saved = localStorage.getItem('theme');
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            const isDark = saved ? saved === 'dark' : prefersDark;

            if (isDark) {
              document.documentElement.classList.add('dark');
              document.documentElement.style.setProperty('--bg-primary', '#0c0c0c');
              document.body.style.backgroundColor = '#0c0c0c';
              document.body.style.color = '#ffffff';
            } else {
              document.documentElement.classList.remove('dark');
              document.documentElement.style.setProperty('--bg-primary', '#ffffff');
              document.body.style.backgroundColor = '#ffffff';
              document.body.style.color = '#000000';
            }
          })();
        </script>
      `
      return html.replace('<head>', `<head>${themeScript}`)
    }
  }
}

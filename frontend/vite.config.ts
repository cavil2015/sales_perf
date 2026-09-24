import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// In local dev, the frontend is on port 5173 and backend on 8080.
// This triggers CORS Preflight (OPTIONS) requests, slowing down dev and requiring backend CORS config.
// We MUST use Vite's dev server proxy to forward `/api` requests directly, bypassing browser CORS entirely.
export default defineConfig({
// Terminal Log Erasure
  // Vite aggressively clears the terminal screen on every HMR update. 
  // If the developer is running C# backend logs or a linter in the same split-terminal or CI/CD, 
  // Vite will maliciously wipe out those critical logs! We MUST disable this behavior.
  clearScreen: false,

  // By default, Vite hardcodes asset paths to the root `/` (e.g. `<script src="/assets/index.js">`).
  // If the company deploys this dashboard in a sub-folder (e.g. `corporate.com/sales-dashboard/`),
  // the app will instantly 404 on all JS/CSS files!
  // We MUST set `base: './'` to enforce relative paths, making the build deployable ANYWHERE.
  base: './',

  plugins: [tailwindcss(), react()],
  
// ), we hacked `console.log = () => {}`. 
  // However, the strings passed to `console.log("Secret...")` are STILL physically present in the JS bundle, 
  // inflating file size and allowing hackers to read them by searching the source code!
  // We MUST configure ESBuild to physically strip all console and debugger statements from the AST during build.
  esbuild: {
    drop: ['console', 'debugger'],
  },

// Environment Variable Silencing (CRA Support)
  // By default, Vite strictly only loads env vars starting with `VITE_`. 
  // If the DevOps team migrated this project from Create React App (CRA) and injects `REACT_APP_API_URL`,
  // Vite will maliciously ignore it, causing the app to query localhost in Production!
  // We MUST expand the `envPrefix` to gracefully accept legacy CRA variables.
  envPrefix: ['VITE_', 'REACT_APP_'],
  
// Docker Network Isolation
  // By default, Vite binds to `127.0.0.1`. If a developer runs `npm run dev` inside a Docker container,
  // the app will be completely inaccessible from the host machine browser!
  // We MUST set `host: true` to bind to `0.0.0.0` for Docker/WSL compatibility.
  server: {
    host: true,
    port: 5173,
    
// Silent Port Drift
    // If port 5173 is occupied, Vite silently switches to 5174. 
    // This breaks Hardcoded OAuth Redirect URIs, Cypress E2E tests, and absolute paths!
    // We MUST enforce `strictPort: true` so it fails loudly if the port is hijacked.
    strictPort: true,
    
// HMR WebSocket Tunnel Collapse
    // When running inside Cloud IDEs (Gitpod, Codespaces) or Reverse Proxy Docker setups,
    // the HMR WebSocket tries to connect to the internal proxy port, failing to upgrade the connection.
    // We MUST force the HMR client port to match the exposed public port.
    hmr: {
      clientPort: 5173,
    },
    
    proxy: {
      '/api': {
        target: 'https://127.0.0.1:7260',
        changeOrigin: true,
        secure: false,
      }
    }
  },

  build: {
// Modern Syntax Crash (Corporate Browser Incompatibility)
    // Vite 5 defaults to `target: 'modules'` (Safari 14+, Edge 88+).
    // In corporate/banking environments, employees often use older locked-down browsers (e.g. Chrome 80).
    // The dashboard would silently fail to load because of unsupported modern JS syntax.
    // We MUST set the target to `es2015` (ES6) to ensure backwards compatibility with 99.9% of corporate PCs.
    target: 'es2015',
    
// Bundle Size Warning Fatigue
    // Vite defaults to a 500kb warning limit. Enterprise libraries (like Recharts) often push vendor chunks
    // slightly past this. Continuous false-positive warnings cause "Warning Fatigue", making developers
    // ignore actual critical build warnings. We bump the limit to a realistic modern HTTP/2 threshold (1000kb).
    chunkSizeWarningLimit: 1000,
    
    // While Vite defaults to false, we explicitly disable sourcemaps in production.
    // Accidentally deploying sourcemaps allows anyone to reconstruct our original unminified TypeScript code,
    // exposing proprietary algorithms and backend endpoints.
    sourcemap: false,
    
// Monolithic Vendor Bundle
    // Vite bundles all `node_modules` into a single massive `vendor.js`.
    // If we update a tiny library, the user's browser cache for the ENTIRE vendor chunk is invalidated!
    // We MUST use manualChunks to split stable libraries (React) from volatile ones (Recharts/Date-fns).
    rollupOptions: {
      output: {
        manualChunks: {
          'react-core': ['react', 'react-dom'],
          'recharts-vendor': ['recharts'],
          'date-vendor': ['date-fns']
        }
      }
    }
  }
})





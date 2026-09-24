import { StrictMode, Component, ErrorInfo, ReactNode, Suspense } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";

// Brittle Module Resolution
// Importing files with hardcoded `.tsx` extensions is an anti-pattern in TypeScript.
// If the file is later migrated to `.jsx` or an `index.ts` directory resolution,
// this import will break. We MUST rely on Vite/TS module resolution without the extension.
import App from "./App";

// Import Hoisting Violation
// In ES6 modules, `import` statements MUST be at the top of the file.
// Previously, the DevTools injection code was placed before `import { createRoot }`.
// While bundlers like Vite/Rollup auto-hoist imports, it breaks standard ESLint rules (`import/first`)
// and can cause unpredictable initialization order during Hot Module Replacement (HMR).

// If users have the React DevTools Chrome Extension installed, they can inspect our production app,
// potentially viewing sensitive internal state, feature flags, or PII passed as props.
// To maintain maximum corporate security, we MUST sabotage the global React DevTools hook in production.
if (import.meta.env.PROD && typeof window !== "undefined") {
  const devToolsHook = (window as any).__REACT_DEVTOOLS_GLOBAL_HOOK__;
  if (typeof devToolsHook === "object" && devToolsHook !== null) {
    for (const key in devToolsHook) {
      if (key === "renderers") devToolsHook[key] = new Map();
      else if (typeof devToolsHook[key] === "function")
        devToolsHook[key] = () => {};
      else devToolsHook[key] = null;
    }
  }

  // Leftover `console.log` or `console.debug` statements from developers can leak
  // business logic, performance metrics, or state transitions to end-users.
  // We MUST strip out standard logs in production to keep the console completely clean.
  console.log = () => {};
  console.debug = () => {};
  console.info = () => {};
  // We leave console.error and console.warn intact for our GlobalErrorBoundary and Telemetry!
}

// Modern Enterprise SPAs MUST track Core Web Vitals, specifically "Long Tasks"
// that block the main thread for >50ms and cause Jank/Input Delay (INP).
// We initialize a PerformanceObserver to track and report UI thread blockages.
if (typeof window !== "undefined" && "PerformanceObserver" in window) {
  try {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        // If a task blocks the main thread for over 50ms, it degrades INP (Interaction to Next Paint)
        if (entry.duration > 50) {
          console.warn(
            `[Performance] Long Task detected: ${entry.duration.toFixed(0)}ms`,
            entry,
          );
        }
      }
    });
    observer.observe({ entryTypes: ["longtask"] });
  } catch (e) {
    // Ignore if not supported by the browser
  }
}

// Promises that reject without a `.catch()` (or unawaited async functions)
// bypass the React ErrorBoundary entirely! They silently fail in the background.
// We MUST attach a global listener to catch and report asynchronous ghost crashes.
if (typeof window !== "undefined") {
  window.addEventListener("unhandledrejection", (event) => {
    // Promise Rejection Default Behavior
    // We explicitly call preventDefault() to stop the browser from throwing a duplicate
    // generic "Uncaught (in promise)" error in the console, keeping logs clean for telemetry.
    event.preventDefault();
    console.error("Unhandled Asynchronous Rejection:", event.reason);
  });
}

class GlobalErrorBoundary extends Component<
  { children: ReactNode },
  { hasError: boolean; error: Error | null }
> {
  constructor(props: { children: ReactNode }) {
    super(props);
    this.state = { hasError: false, error: null };
  }
  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }
  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Critical UI Crash:", error, errorInfo);
  }
  render() {
    if (this.state.hasError) {
      return (
        <div className="flex h-screen w-full items-center justify-center bg-gray-50 p-6">
          <div className="rounded-xl bg-white p-8 shadow-lg max-w-lg w-full border-t-4 border-red-500">
            <h2 className="text-2xl font-bold text-slate-900 mb-4">
              Application Crashed
            </h2>
            <p className="text-slate-600 mb-4">
              A critical rendering error occurred. Please refresh the page.
            </p>
            <pre className="bg-slate-100 p-4 rounded text-xs text-red-600 overflow-auto max-h-40">
              {this.state.error?.message || "Unknown Error"}
            </pre>
            <button
              onClick={() => window.location.reload()}
              className="mt-6 bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded transition-colors"
            >
              Reload Page
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

const rootElement = document.getElementById("root");
if (!rootElement) {
  document.body.innerHTML =
    '<div style="color:red;padding:20px;text-align:center;font-family:sans-serif;">Fatal Error: Root DOM element missing. Check index.html.</div>';
  throw new Error("Failed to find the root element.");
}

// Pre-Mount Synchronous Crashes
// The ErrorBoundary catches React rendering errors, and `unhandledrejection` catches async errors,
// but synchronous errors that happen OUTSIDE React (e.g. 3rd party scripts, or errors during initial parsing)
// bypass both! We MUST attach a global `error` listener to catch absolutely everything.
if (typeof window !== "undefined") {
  window.addEventListener("error", (event) => {
    console.error("Global Synchronous Crash:", event.message);
  });
}

// Microfrontend ID Collisions (useId Namespace)
// React 18 introduced `useId()` to generate unique DOM IDs (like `:r0:`).
// If this dashboard is ever embedded into a larger Corporate Portal (Microfrontend architecture),
// the host React app and our React app will both generate `:r0:`, causing ID collisions and breaking A11y!
// We MUST pass an `identifierPrefix` to `createRoot` to namespace all our generated IDs.
createRoot(rootElement, {
  identifierPrefix: "sales-perf-",
  // React 18 Recoverable Error Telemetry
  // React 18 introduced automatic error recovery (e.g. retrying rendering).
  // By default, it silently calls console.error. We MUST intercept this to track it in our telemetry.
  onRecoverableError: (error, errorInfo) => {
    console.warn("React Recoverable Error (Silently Fixed):", error, errorInfo);
  },
}).render(
  <StrictMode>
    <GlobalErrorBoundary>
      {/* If `App` or any child later uses React 18 `lazy()` or `use()` for code splitting,
          it will throw a Promise that crashes the app if there's no Suspense boundary.
          We MUST wrap the app in Suspense to future-proof the architecture for code splitting. */}
      <Suspense
        fallback={
          <div className="p-8 text-center text-slate-500">
            Loading Application...
          </div>
        }
      >
        <App />
      </Suspense>
    </GlobalErrorBoundary>
  </StrictMode>,
);

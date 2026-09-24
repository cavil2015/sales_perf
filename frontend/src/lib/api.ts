// Vite Build-Time Env Lock-in
// Vite statically replaces `import.meta.env` during build. If we deploy a Docker container
// and try to pass `-e VITE_API_URL=...` at runtime, it will be completely ignored!
// To support 12-Factor App methodology (configure once, deploy anywhere), we MUST check
// the `window` object first, allowing a startup script in `index.html` to inject runtime variables.
const runtimeConfig =
  typeof window !== "undefined" ? (window as any).__RUNTIME_CONFIG__ : {};
export const API_URL =
  runtimeConfig?.VITE_API_URL ||
  import.meta.env.VITE_API_URL ||
  (import.meta.env.PROD ? "/api" : "/api");

async function handleResponse(res: Response, defaultMessage: string) {
  if (!res.ok) {
    let errorText = await res.text().catch(() => "No response body");

    // If the backend returns a structured ProblemDetails JSON (e.g. `{"title": "Validation Failed"}`),
    // displaying the raw JSON string in the React Error Banner looks like a hack to the end-user.
    // We MUST attempt to parse it and extract human-readable fields (`message`, `title`, or `detail`).
    try {
      const parsed = JSON.parse(errorText);
      errorText = parsed.detail || parsed.title || parsed.message || errorText;
    } catch {
      /* Not JSON, fallback to raw text */
    }

    throw new Error(`${defaultMessage} (HTTP ${res.status}): ${errorText}`);
  }

  // 204 No Content (Empty Body JSON Parse Crash)
  // Blindly calling `res.json()` crashes with `SyntaxError: Unexpected end of JSON input`
  // if the server returns 200 OK or 204 No Content with an empty body!
  // We MUST read as text first, and parse only if content exists.
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

function buildUrl(endpoint: string, from?: string, to?: string) {
  const params = new URLSearchParams();

  // If the UI passes strings with leading/trailing spaces (e.g. from a copy-paste or rogue state),
  // `URLSearchParams` will literally encode the space as `%20`.
  // The C# backend `DateTimeOffset.Parse` will fail to parse `2024-01-01 %20` and throw a 500 Error.
  // We MUST trim whitespace before sending parameters over the wire.
  if (from && from.trim()) params.append("from", from.trim());
  if (to && to.trim()) params.append("to", to.trim());

  const qs = params.toString();

  // If API_URL is `http://host/api/` and endpoint is `/analytics`,
  // blind concatenation creates `http://host/api//analytics`.
  // Some strict reverse proxies (like NGINX or AWS ALB) will block double-slashes as a security risk.
  const cleanBase = API_URL.replace(/\/$/, "");
  const cleanEndpoint = endpoint.startsWith("/") ? endpoint : `/${endpoint}`;

  return qs
    ? `${cleanBase}${cleanEndpoint}?${qs}`
    : `${cleanBase}${cleanEndpoint}`;
}

// Zombie Connection / Infinite Request Deadlock
// If the backend accepts the connection but hangs (e.g. database deadlock),
// `fetch` will wait up to 300 seconds before failing. The UI will spin forever.
// We MUST enforce a strict API timeout using `AbortSignal.timeout()` (or a fallback).
// We combine the component's AbortSignal (onUnmount/onChange) with the timeout signal.
function createTimeoutSignal(
  timeoutMs: number,
  customSignal?: AbortSignal,
): AbortSignal {
  const controller = new AbortController();

  // Wire up the custom signal (from App.tsx) to abort this local controller
  if (customSignal) {
    if (customSignal.aborted) controller.abort(customSignal.reason);
    else
      customSignal.addEventListener("abort", () =>
        controller.abort(customSignal.reason),
      );
  }

  // Set the fallback timeout
  const timeoutId = setTimeout(
    () => controller.abort(new Error("API Timeout")),
    timeoutMs,
  );

  // Clean up timeout if successfully completed or aborted externally
  controller.signal.addEventListener("abort", () => clearTimeout(timeoutId));

  return controller.signal;
}

// Mobile users experience micro-disconnects (e.g. passing through a tunnel).
// A single failed GET request shouldn't crash the dashboard.
// We implement a resilient fetch wrapper with automatic retries for GET requests.
async function fetchWithRetry(
  url: string,
  options: RequestInit,
  retries: number = 1,
): Promise<Response> {
  try {
    const timeoutSignal = createTimeoutSignal(15000, options.signal as any);
    const res = await fetch(url, { ...options, signal: timeoutSignal });

    // Don't retry on 4xx Client Errors (they won't fix themselves)
    if (!res.ok && res.status >= 500 && retries > 0) {
      throw new Error(`Server Error ${res.status}`);
    }
    return res;
  } catch (err: any) {
    // Do not retry if the component unmounted (AbortError)
    if (err.name === "AbortError" && !err.message?.includes("Timeout"))
      throw err;

    if (retries > 0) {
      await new Promise((res) => setTimeout(res, 1000)); // 1s backoff
      return fetchWithRetry(url, options, retries - 1);
    }
    throw err;
  }
}

export const fetchKpis = async (
  from?: string,
  to?: string,
  signal?: AbortSignal,
) => {
  const res = await fetchWithRetry(buildUrl("/analytics/kpi", from, to), {
    signal,
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Cache-Control": "no-cache, no-store, must-revalidate",
      Pragma: "no-cache",
    },
  });
  return handleResponse(res, "Failed to fetch KPIs");
};

export const fetchManagersRating = async (
  from?: string,
  to?: string,
  signal?: AbortSignal,
) => {
  const res = await fetchWithRetry(
    buildUrl("/analytics/managers-rating", from, to),
    {
      signal,
      credentials: "include",
      headers: {
        Accept: "application/json",
        "Cache-Control": "no-cache, no-store, must-revalidate",
        Pragma: "no-cache",
      },
    },
  );
  return handleResponse(res, "Failed to fetch managers rating");
};

export const fetchChartData = async (
  from?: string,
  to?: string,
  signal?: AbortSignal,
) => {
  const res = await fetchWithRetry(buildUrl("/analytics/chart", from, to), {
    signal,
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Cache-Control": "no-cache, no-store, must-revalidate",
      Pragma: "no-cache",
    },
  });
  return handleResponse(res, "Failed to fetch chart data");
};

export const fetchRecentSales = async (signal?: AbortSignal) => {
  const res = await fetchWithRetry(buildUrl("/analytics/recent"), {
    signal,
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Cache-Control": "no-cache, no-store, must-revalidate",
      Pragma: "no-cache",
    },
  });
  return handleResponse(res, "Failed to fetch recent sales");
};

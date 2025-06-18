/**
 * Time:2025/5/10 13:08 02
 * Name:global.d.ts
 * Path:src/types
 * ProjectName:ClientApp
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */


// global.d.ts
interface WebView2 {
  postMessage: (message: string) => void;
  addEventListener: <T>(type: "message", handler: (event: T) => void) => void;
}

interface ChromeWithWebView {
  webview: WebView2;
}

interface Window {
  chrome?: ChromeWithWebView;
}

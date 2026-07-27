import "./globals.css";

export const metadata = {
  title: "Houscaper",
  description: "A geometry-first Townscaper-style Rhino tile playground",
};

export default function RootLayout({ children }) {
  return (
    <html lang="ko">
      <body>{children}</body>
    </html>
  );
}

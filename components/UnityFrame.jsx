"use client";

import { useEffect, useRef, useState } from "react";

/**
 * Hosts the Unity WebGL build in an iframe so its canvas, input handling and
 * loader stay isolated from the Next.js app. The build is served statically
 * from /unity, which `npm run build:unity` writes into public/unity.
 */
export default function UnityFrame({ src = "/unity/index.html" }) {
  const frameRef = useRef(null);
  const [status, setStatus] = useState("loading");

  useEffect(() => {
    let cancelled = false;

    fetch(src, { method: "HEAD" })
      .then((response) => {
        if (cancelled) return;
        setStatus(response.ok ? "ready" : "missing");
      })
      .catch(() => {
        if (!cancelled) setStatus("missing");
      });

    return () => {
      cancelled = true;
    };
  }, [src]);

  if (status === "missing") {
    return (
      <div className="unity-missing">
        <h1>Houscaper</h1>
        <p>Unity WebGL 빌드가 아직 없습니다.</p>
        <pre>
{`npm run build:unity
# 또는 Unity 에디터에서 Houscaper ▸ Build WebGL`}
        </pre>
        <p className="dim">
          빌드는 <code>public/unity/</code> 에 생성되며 이 페이지가 자동으로 불러옵니다.
        </p>
      </div>
    );
  }

  return (
    <iframe
      ref={frameRef}
      className="unity-frame"
      src={src}
      title="Houscaper"
      allow="autoplay; fullscreen"
    />
  );
}

// pro-ui.umd.js 入口：自包含 React19 + react-dom/client + 全量 pro-ui
// 同时把 React / ReactDOM 挂到 window，供 cshtml 里直接 createElement / createRoot 使用
import * as ReactNS from 'react';
import * as ReactDOMClientNS from 'react-dom/client';

if (typeof window !== 'undefined') {
  window.React = ReactNS;
  window.ReactDOM = ReactDOMClientNS;
}

export * from '@dangbt/pro-ui';
export { ReactNS as React, ReactDOMClientNS as ReactDOM };

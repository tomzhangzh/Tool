// src/components/AvatarImage/index.jsx
import React, { useState, useEffect } from 'react';
import { Image } from '@tarojs/components';
import { resolveFileURL } from '../../utils/cloud';
import './index.scss';

/**
 * 通用图片组件
 * - 自动处理 fileID (cloud://) 到可访问 URL 的转换
 * - 支持 http(s) URL 直接使用
 * - 结果会被缓存
 * - 支持懒加载 (lazyLoad)
 * - 支持骨架屏占位效果
 * 
 * Props:
 * - src: fileID 或 URL
 * - className: 自定义样式类名
 * - mode: 图片裁剪模式 (aspectFill | aspectFit | widthFix | heightFix)
 * - lazyLoad: 是否启用懒加载 (默认 true)
 * - showSkeleton: 是否显示骨架屏 (默认 true)
 * - size: 加载中的占位符大小
 */
export default function AvatarImage({ 
  src, 
  className = '', 
  mode = 'aspectFill', 
  lazyLoad = true,
  showSkeleton = true,
  ...rest 
}) {
  const [resolvedUrl, setResolvedUrl] = useState('');
  const [error, setError] = useState(false);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    const resolve = async () => {
      if (!src || typeof src !== 'string' || src.trim() === '') {
        setResolvedUrl('');
        return;
      }
      
      const trimmed = src.trim();
      
      // 已经是 http(s) URL
      if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
        setResolvedUrl(trimmed);
        return;
      }
      
      // fileID 格式，需要解析
      if (trimmed.startsWith('cloud://')) {
        try {
          const url = await resolveFileURL(trimmed);
          setResolvedUrl(url);
          setError(!url);
        } catch (e) {
          console.error('[AvatarImage] resolve error:', trimmed, e);
          setError(true);
        }
        return;
      }
      
      // 其他情况，直接使用
      setResolvedUrl(trimmed);
    };
    
    resolve();
  }, [src]);

  if (!src || typeof src !== 'string' || src.trim() === '') {
    return null;
  }

  if (error) {
    return (
      <div className={`avatar-placeholder ${className}`} {...rest}>
        {src.includes('/') ? '?' : src[0]?.toUpperCase() || '?'}
      </div>
    );
  }

  if (!resolvedUrl) {
    if (showSkeleton) {
      return (
        <div className={`avatar-skeleton ${className}`} {...rest}>
          <div className='skeleton-animation' />
        </div>
      );
    }
    return null;
  }

  return (
    <div className={`avatar-image-wrapper ${className}`}>
      {showSkeleton && !loaded && (
        <div className='avatar-skeleton skeleton-animation' />
      )}
      <Image
        src={resolvedUrl}
        className={`${className} ${loaded ? 'image-loaded' : 'image-loading'}`}
        mode={mode}
        lazyLoad={lazyLoad}
        onLoad={() => setLoaded(true)}
        onError={() => { setError(true); setLoaded(true); }}
        {...rest}
      />
    </div>
  );
}

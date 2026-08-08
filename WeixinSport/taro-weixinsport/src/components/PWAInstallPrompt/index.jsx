// src/components/PWAInstallPrompt/index.jsx
import React, { useState, useEffect } from 'react';
import { View, Text } from '@tarojs/components';
import Taro from '@tarojs/taro';
import './index.scss';

/**
 * 添加到主屏幕引导组件
 * - 检测是否可以安装 PWA
 * - 显示安装引导提示
 */
export default function PWAInstallPrompt() {
  const [deferredPrompt, setDeferredPrompt] = useState(null);
  const [isStandalone, setIsStandalone] = useState(false);
  const [isIOS, setIsIOS] = useState(false);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // 检测是否已独立运行（已安装）
    const checkStandalone = () => {
      const standalone = window.matchMedia('(display-mode: standalone)').matches 
        || window.navigator.standalone === true;
      setIsStandalone(standalone);
      if (standalone) {
        setIsVisible(false);
      }
    };

    // 检测 iOS
    const checkIOS = () => {
      const iOS = /iPad|iPhone|iPod/.test(window.navigator.userAgent);
      setIsIOS(iOS);
    };

    // 拦截安装提示
    const handleBeforeInstallPrompt = (e) => {
      e.preventDefault();
      setDeferredPrompt(e);
      // 延迟显示，让用户先看到页面
      setTimeout(() => {
        if (!isStandalone) {
          setIsVisible(true);
        }
      }, 3000);
    };

    // 监听安装完成
    const handleAppInstalled = () => {
      setIsVisible(false);
      setDeferredPrompt(null);
      Taro.showToast({ title: '安装成功', icon: 'success' });
    };

    checkStandalone();
    checkIOS();

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.addEventListener('appinstalled', handleAppInstalled);

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
      window.removeEventListener('appinstalled', handleAppInstalled);
    };
  }, [isStandalone]);

  const handleInstall = async () => {
    if (!deferredPrompt) {
      // iOS 需要手动引导
      Taro.showModal({
        title: '添加到主屏幕',
        content: isIOS 
          ? '请点击浏览器"分享"按钮，然后选择"添加到主屏幕"' 
          : '请点击浏览器菜单，选择"安装应用"或"添加到主屏幕"',
        showCancel: false,
        confirmText: '我知道了'
      });
      setIsVisible(false);
      // 保存状态，7 天内不再提示
      Taro.setStorageSync('pwa_prompt_hide_until', Date.now() + 7 * 24 * 60 * 60 * 1000);
      return;
    }

    deferredPrompt.prompt();
    const choiceResult = await deferredPrompt.userChoice;
    
    if (choiceResult.outcome === 'accepted') {
      setIsVisible(false);
    }
    
    setDeferredPrompt(null);
  };

  const handleDismiss = () => {
    setIsVisible(false);
    // 保存状态，7 天内不再提示
    Taro.setStorageSync('pwa_prompt_hide_until', Date.now() + 7 * 24 * 60 * 60 * 1000);
  };

  // 检查是否应该显示（7 天冷却）
  useEffect(() => {
    if (!isVisible) return;
    const hideUntil = Taro.getStorageSync('pwa_prompt_hide_until');
    if (hideUntil && Date.now() < hideUntil) {
      setIsVisible(false);
    }
  }, [isVisible]);

  if (!isVisible || isStandalone) {
    return null;
  }

  return (
    <View className='pwa-prompt'>
      <View className='pwa-prompt-content'>
        <View className='pwa-icon'>📱</View>
        <View className='pwa-text'>
          <Text className='pwa-title'>添加到主屏幕</Text>
          <Text className='pwa-desc'>全屏体验，无浏览器边框</Text>
        </View>
        <View className='pwa-actions'>
          <View className='pwa-btn pwa-btn-primary' onClick={handleInstall}>
            <Text>添加</Text>
          </View>
          <View className='pwa-btn pwa-btn-secondary' onClick={handleDismiss}>
            <Text>稍后</Text>
          </View>
        </View>
      </View>
    </View>
  );
}

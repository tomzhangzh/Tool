// src/utils/compress.js
// 图片压缩工具 - 将图片压缩到指定大小以下（默认 1MB）

const MAX_SIZE = 1024 * 1024; // 1MB

/**
 * 压缩图片（H5 环境）- 返回 Blob 对象
 * @param {File|Blob} file 图片文件对象
 * @param {number} maxSize 目标大小（字节），默认 1MB
 * @returns {Promise<Blob>} 返回压缩后的 Blob
 */
export const compressImageH5 = (file, maxSize = MAX_SIZE) => {
  return new Promise((resolve, reject) => {
    if (!(file instanceof Blob)) {
      reject(new Error('需要传入 File/Blob 对象'));
      return;
    }

    if (file.size <= maxSize) {
      // 图片已小于目标大小，直接返回
      resolve(file);
      return;
    }

    const img = new Image();
    const url = URL.createObjectURL(file);

    img.onload = () => {
      URL.revokeObjectURL(url);
      compressByCanvas(img, file.type, maxSize)
        .then(resolve)
        .catch(reject);
    };

    img.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error('加载图片失败'));
    };

    img.src = url;
  });
};

/**
 * 通过 Canvas 压缩图片 - 返回 Blob
 */
const compressByCanvas = (img, mimeType, maxSize) => {
  return new Promise((resolve, reject) => {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    // 计算初始尺寸
    let width = img.width;
    let height = img.height;

    // 如果图片尺寸过大，先按比例缩小
    const maxDim = 2048;
    if (width > maxDim || height > maxDim) {
      const scale = maxDim / Math.max(width, height);
      width = Math.round(width * scale);
      height = Math.round(height * scale);
    }

    canvas.width = width;
    canvas.height = height;

    // 按质量逐步压缩
    const tryCompress = (currentQuality) => {
      ctx.clearRect(0, 0, width, height);
      ctx.drawImage(img, 0, 0, width, height);

      canvas.toBlob(
        (blob) => {
          if (!blob) {
            reject(new Error('压缩失败'));
            return;
          }

          if (blob.size <= maxSize || currentQuality <= 0.1) {
            // 压缩完成，返回 Blob
            resolve(blob);
          } else {
            // 继续降低质量
            tryCompress(currentQuality - 0.1);
          }
        },
        mimeType || 'image/jpeg',
        currentQuality
      );
    };

    tryCompress(0.9);
  });
};

/**
 * 将 Blob 转换为 base64（用于预览等场景）
 */
export const blobToBase64 = (blob) => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => resolve(e.target.result);
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
};

export default {
  compressImageH5,
  blobToBase64,
};

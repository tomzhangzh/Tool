// cloudfunctions/uploadAvatar/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });

/**
 * 头像上传云函数
 * 接收 base64 图片数据，上传到云存储，返回可访问的 URL
 */
exports.main = async (event, context) => {
  const { fileContent, cloudPath, fileType = 'image/jpeg' } = event;

  if (!fileContent) {
    return { success: false, error: '缺少文件内容' };
  }

  if (!cloudPath) {
    return { success: false, error: '缺少存储路径' };
  }

  try {
    // 处理 base64 数据
    let fileBuffer;
    if (fileContent.startsWith('data:')) {
      // 格式: data:image/jpeg;base64,/9j/4AAQ...
      const base64Data = fileContent.replace(/^data:image\/\w+;base64,/, '');
      fileBuffer = Buffer.from(base64Data, 'base64');
    } else {
      // 纯 base64
      fileBuffer = Buffer.from(fileContent, 'base64');
    }

    // 限制文件大小（1MB）
    const maxSize = 1024 * 1024;
    if (fileBuffer.length > maxSize) {
      return { success: false, error: `文件过大，最大支持 ${maxSize / 1024}KB` };
    }

    // 上传到云存储
    const uploadResult = await cloud.uploadFile({
      cloudPath: cloudPath,
      fileContent: fileBuffer,
    });

    // 获取临时下载链接（有效期2小时）
    const tempUrlResult = await cloud.getTempFileURL({
      fileList: [uploadResult.fileID],
    });

    const fileURL = tempUrlResult.fileList[0]?.tempFileURL || '';

    return {
      success: true,
      fileID: uploadResult.fileID,
      url: fileURL,
    };
  } catch (err) {
    console.error('上传失败:', err);
    return {
      success: false,
      error: err.message || '上传失败',
    };
  }
};

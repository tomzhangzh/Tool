// cloudfunctions/like/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

// 按 username 查找用户
const findUser = async (username) => {
  if (!username) return null;
  const q = await db.collection('users').where({ username }).get();
  return q.data.length ? q.data[0] : null;
};

exports.main = async (event, context) => {
  const { action, username, targetId, targetType } = event;

  try {
    // 点赞/取消点赞
    if (action === 'toggle') {
      if (!username || !targetId) {
        return { code: 1, message: '参数错误' };
      }

      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在' };

      // 检查是否已点赞
      const likeQ = await db.collection('likes').where({
        targetId,
        targetType: targetType || 'checkin',
        username
      }).get();

      if (likeQ.data.length > 0) {
        // 已点赞 → 取消
        await db.collection('likes').doc(likeQ.data[0]._id).remove();
        
        // 更新打卡记录的点赞数
        const checkinQ = await db.collection('checkins').doc(targetId).get();
        const currentLikes = checkinQ.data.likeCount || 0;
        await db.collection('checkins').doc(targetId).update({
          data: { likeCount: Math.max(0, currentLikes - 1) }
        });

        return { code: 0, data: { liked: false, likeCount: Math.max(0, currentLikes - 1) } };
      } else {
        // 未点赞 → 添加
        await db.collection('likes').add({
          data: {
            targetId,
            targetType: targetType || 'checkin',
            username,
            userName: user.name,
            avatar: user.avatar,
            createTime: Date.now()
          }
        });

        // 更新打卡记录的点赞数
        const checkinQ = await db.collection('checkins').doc(targetId).get();
        const currentLikes = checkinQ.data.likeCount || 0;
        await db.collection('checkins').doc(targetId).update({
          data: { likeCount: currentLikes + 1 }
        });

        return { code: 0, data: { liked: true, likeCount: currentLikes + 1 } };
      }
    }

    // 批量查询点赞状态
    if (action === 'batchCheck') {
      const { targetIds, targetType: tt } = event;
      if (!username || !targetIds || !Array.isArray(targetIds)) {
        return { code: 1, message: '参数错误' };
      }

      const likeQ = await db.collection('likes').where({
        targetId: _.in(targetIds),
        targetType: tt || 'checkin',
        username
      }).get();

      // 返回点赞状态映射
      const likedMap = {};
      likeQ.data.forEach(like => {
        likedMap[like.targetId] = true;
      });

      return { code: 0, data: likedMap };
    }

    // 获取某条记录的点赞列表
    if (action === 'list') {
      const { targetId, targetType: tt, page = 1, pageSize = 20 } = event;
      if (!targetId) {
        return { code: 1, message: '参数错误' };
      }

      const countResult = await db.collection('likes').where({
        targetId,
        targetType: tt || 'checkin'
      }).count();
      const total = countResult.total;

      const q = await db.collection('likes')
        .where({
          targetId,
          targetType: tt || 'checkin'
        })
        .orderBy('createTime', 'desc')
        .skip((page - 1) * pageSize)
        .limit(pageSize)
        .get();

      return { code: 0, data: { list: q.data, total } };
    }

    // 我的点赞历史
    if (action === 'mine') {
      const { page = 1, pageSize = 20 } = event;
      if (!username) {
        return { code: 1, message: '参数错误' };
      }

      const countResult = await db.collection('likes').where({ username }).count();
      const total = countResult.total;
      const q = await db.collection('likes')
        .where({ username })
        .orderBy('createTime', 'desc')
        .skip((page - 1) * pageSize)
        .limit(pageSize)
        .get();

      return { code: 0, data: { list: q.data, total } };
    }

    return { code: 1, message: '未知操作' };
  } catch (e) {
    console.error('like error', e);
    return { code: 1, message: String(e.message || e) };
  }
};

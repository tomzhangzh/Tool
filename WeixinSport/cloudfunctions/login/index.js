// cloudfunctions/login/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

// 登录 / 绑定角色 / 获取个人信息 / 更新个人信息
exports.main = async (event, context) => {
  const wxCtx = cloud.getWXContext();
  const openid = wxCtx.OPENID;
  const action = event.action || 'login';

  try {
    if (action === 'login') {
      // 返回 openid，前端据此决定下一步
      // 同时 upsert 用户记录
      const userCol = db.collection('users');
      const existing = await userCol.where({ openid }).get();
      let user = existing.data[0];
      if (!user) {
        const now = Date.now();
        const addRes = await userCol.add({
          data: {
            openid,
            role: '',
            name: '',
            avatar: '',
            weight: 30,
            classIds: [],
            childOpenid: '',
            parentOpenids: [],
            createTime: now,
            updateTime: now
          }
        });
        user = (await userCol.doc(addRes._id).get()).data;
      }
      return { code: 0, data: { openid, ...user } };
    }

    if (action === 'bindRole') {
      // 注册/绑定角色，由 login 页调用
      const { role, name, avatar, weight, classCode, childName } = event;
      const update = {
        role,
        name,
        avatar: avatar || '',
        updateTime: Date.now()
      };
      if (role === 'student') {
        update.weight = weight || 30;
      }

      // 学生加入班级
      let joinedClassIds = [];
      if (role === 'student' && classCode) {
        const classCol = db.collection('classes');
        const cls = await classCol.where({ code: classCode, status: 'active' }).get();
        if (!cls.data.length) {
          return { code: 1, message: '邀请码无效或班级已关闭' };
        }
        const c = cls.data[0];
        joinedClassIds = [c._id];
        update.classIds = joinedClassIds;
        // 班级成员表登记
        await db.collection('class_members').add({
          data: {
            classId: c._id,
            openid,
            role: 'student',
            joinTime: Date.now()
          }
        });
      }

      // 家长绑定孩子
      if (role === 'parent' && childName) {
        // 根据姓名找孩子（同名可能冲突，需老师人工核对）
        const stu = await db.collection('users').where({ role: 'student', name: childName }).get();
        if (!stu.data.length) {
          return { code: 1, message: '未找到该姓名的学生，请确认孩子已注册' };
        }
        // 取第一个匹配（实际可加班级筛选）
        const child = stu.data[0];
        update.childOpenid = child.openid;
        // 把家长 openid 加入孩子的 parentOpenids
        await db.collection('users').doc(child._id).update({
          data: { parentOpenids: _.push([openid]) }
        });
      }

      // 写入更新
      const userCol = db.collection('users');
      const existing = await userCol.where({ openid }).get();
      let userId;
      if (existing.data.length) {
        await userCol.doc(existing.data[0]._id).update({ data: update });
        userId = existing.data[0]._id;
      } else {
        const addRes = await userCol.add({
          data: {
            openid,
            classIds: joinedClassIds,
            createTime: Date.now(),
            ...update
          }
        });
        userId = addRes._id;
      }
      const finalUser = (await userCol.doc(userId).get()).data;
      return { code: 0, data: finalUser };
    }

    if (action === 'getProfile') {
      // 个人中心：返回汇总统计 + 奖项数
      const userQ = await db.collection('users').where({ openid }).get();
      if (!userQ.data.length) return { code: 1, message: '用户不存在' };
      const user = userQ.data[0];

      // 视角：家长看孩子
      let targetOpenid = openid;
      if (user.role === 'parent' && user.childOpenid) {
        targetOpenid = user.childOpenid;
      }

      // 汇总打卡数据
      const agg = db.collection('checkins').aggregate();
      const statRes = await agg.match({ openid: targetOpenid })
        .group({
          _id: null,
          totalDuration: { $sum: '$duration' },
          totalCalorie: { $sum: '$calorie' },
          totalCheckins: { $sum: 1 }
        })
        .end();
      const summary = statRes.list[0] || { totalDuration: 0, totalCalorie: 0, totalCheckins: 0 };

      // 坚持天数（去重打卡日）
      const dayAgg = db.collection('checkins').aggregate();
      const dayRes = await dayAgg.match({ openid: targetOpenid })
        .group({ _id: '$dateStr' })
        .count('days')
        .end();
      summary.totalDays = dayRes.list.length || 0;

      // 奖项数
      const awardCount = (await db.collection('awards').where({ openid: targetOpenid }).count()).total;

      return { code: 0, data: { summary, awardCount } };
    }

    if (action === 'updateProfile') {
      const update = { ...event };
      delete update.action;
      update.updateTime = Date.now();
      const userQ = await db.collection('users').where({ openid }).get();
      if (userQ.data.length) {
        await db.collection('users').doc(userQ.data[0]._id).update({ data: update });
      }
      const updated = (await db.collection('users').doc(userQ.data[0]._id).get()).data;
      return { code: 0, data: updated };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('login cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};

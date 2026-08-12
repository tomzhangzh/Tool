// cloudfunctions/login/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

// 登录 / 绑定角色 / 获取个人信息 / 更新个人信息
// 统一使用 username 作为用户关联键，openid 仅用于小程序原生登录场景
exports.main = async (event, context) => {
  const action = event.action || 'login';
  const username = event.username;

  if (action !== 'login' && action !== 'uploadAvatar' && action !== 'dedupUsers') {
    if (!username) {
      return { code: 1, message: '缺少用户标识' };
    }
  }

  try {
    // ========== 小程序原生登录（无账号体系）==========
    if (action === 'login') {
      const wxCtx = cloud.getWXContext();
      const openid = wxCtx.OPENID || event._uid;
      if (!openid) {
        return { code: 1, message: '未获取到用户身份' };
      }
      const userCol = db.collection('users');
      // 小程序端登录时按 openid 查找（兼容旧数据）
      const existing = await userCol.where({ openid }).orderBy('createTime', 'asc').get();
      const user = existing.data[0];
      return { code: 0, data: { openid, ...(user || {}) } };
    }

    // ========== 用户名密码注册 ==========
    if (action === 'register') {
      const { password, role, name, avatar, weight, classCode, teacherCode } = event;
      if (!username || !password) {
        return { code: 1, message: '用户名和密码不能为空' };
      }

      // 检查用户名是否已存在
      const userCol = db.collection('users');
      const existUser = await userCol.where({ username }).get();
      if (existUser.data.length > 0) {
        return { code: 1, message: '用户名已存在' };
      }

      // 老师注册需要验证注册码
      if (role === 'teacher') {
        const TEACHER_CODE = 'hydxc';
        if (!teacherCode || teacherCode.trim().toLowerCase() !== TEACHER_CODE) {
          return { code: 1, message: '老师注册码错误' };
        }
      }

      // 学生注册必须提供班级邀请码
      let joinedClassIds = [];
      if (role === 'student') {
        if (!classCode || !classCode.trim()) {
          return { code: 1, message: '学生注册必须输入班级邀请码' };
        }
        const classCol = db.collection('classes');
        const cls = await classCol.where({ code: classCode.trim(), status: 'active' }).get();
        if (!cls.data.length) {
          return { code: 1, message: '邀请码无效或班级已关闭' };
        }
        joinedClassIds = [cls.data[0]._id];
      }

      // 创建用户
      const addData = {
        username,
        password,
        role: role || 'student',
        name: name || username,
        avatar: avatar || '',
        weight: role === 'student' ? (weight || 60) : 60,
        createTime: Date.now(),
        updateTime: Date.now()
      };

      // 家长绑定孩子
      if (role === 'parent' && event.childName) {
        const stu = await userCol.where({ role: 'student', name: event.childName }).get();
        if (!stu.data.length) {
          return { code: 1, message: '未找到该姓名的学生，请确认孩子已注册' };
        }
        addData.childUsername = stu.data[0].username;
      }

      const addRes = await userCol.add({ data: addData });

      // 学生加入班级：写入 class_members 表
      if (role === 'student' && joinedClassIds.length > 0) {
        await db.collection('class_members').add({
          data: {
            classId: joinedClassIds[0],
            username,
            role: 'student',
            joinTime: Date.now()
          }
        });
      }

      const newUser = (await userCol.doc(addRes._id).get()).data;
      return { code: 0, data: newUser };
    }

    // ========== 用户名密码登录 ==========
    if (action === 'loginByAccount') {
      const { password } = event;
      if (!username || !password) {
        return { code: 1, message: '用户名和密码不能为空' };
      }

      const userCol = db.collection('users');
      const userQ = await userCol.where({ username }).get();
      if (!userQ.data.length) {
        return { code: 1, message: '用户名或密码错误' };
      }

      const user = userQ.data[0];
      if (user.password !== password) {
        return { code: 1, message: '用户名或密码错误' };
      }

      const result = { ...user, username: user.username || username };
      console.log('[loginByAccount] 登录成功:', { username, role: user.role });
      return { code: 0, data: result };
    }

    // ========== 绑定角色（补充注册信息）==========
    if (action === 'bindRole') {
      const { role, name, avatar, weight, classCode, childName } = event;
      const userCol = db.collection('users');
      const existing = await userCol.where({ username }).orderBy('createTime', 'asc').get();

      if (!existing.data.length) {
        return { code: 1, message: '用户不存在，请先注册' };
      }

      const user = existing.data[0];
      const update = {
        role,
        name: name || user.name,
        avatar: avatar || user.avatar || '',
        updateTime: Date.now()
      };
      if (role === 'student') {
        update.weight = weight || 60;
      }

      // 学生加入班级
      if (role === 'student' && classCode) {
        const classCol = db.collection('classes');
        const cls = await classCol.where({ code: classCode, status: 'active' }).get();
        if (!cls.data.length) {
          return { code: 1, message: '邀请码无效或班级已关闭' };
        }
        // 检查是否已加入
        const memberQ = await db.collection('class_members').where({
          classId: cls.data[0]._id,
          username
        }).get();
        if (!memberQ.data.length) {
          await db.collection('class_members').add({
            data: {
              classId: cls.data[0]._id,
              username,
              role: 'student',
              joinTime: Date.now()
            }
          });
        }
      }

      // 家长绑定孩子
      if (role === 'parent' && childName) {
        const stu = await userCol.where({ role: 'student', name: childName }).get();
        if (!stu.data.length) {
          return { code: 1, message: '未找到该姓名的学生，请确认孩子已注册' };
        }
        update.childUsername = stu.data[0].username;
      }

      await userCol.doc(user._id).update({ data: update });
      const finalUser = (await userCol.doc(user._id).get()).data;
      return { code: 0, data: finalUser };
    }

    // ========== 获取个人信息（含汇总统计）==========
    if (action === 'getProfile') {
      const userCol = db.collection('users');
      const userQ = await userCol.where({ username }).get();
      if (!userQ.data.length) return { code: 1, message: '用户不存在' };
      const user = userQ.data[0];

      // 家长视角：查看孩子数据
      let targetUsername = user.username;
      if (user.role === 'parent' && user.childUsername) {
        targetUsername = user.childUsername;
      }

      // 汇总打卡数据
      const agg = db.collection('checkins').aggregate();
      const statRes = await agg.match({ username: targetUsername })
        .group({
          _id: null,
          totalDuration: { $sum: '$duration' },
          totalCalorie: { $sum: '$calorie' },
          totalCheckins: { $sum: 1 }
        })
        .end();
      const summary = statRes.list[0] || { totalDuration: 0, totalCalorie: 0, totalCheckins: 0 };

      // 坚持天数
      const dayAgg = db.collection('checkins').aggregate();
      const dayRes = await dayAgg.match({ username: targetUsername })
        .group({ _id: '$dateStr' })
        .count('days')
        .end();
      summary.totalDays = dayRes.list.length || 0;

      // 奖项数
      const awardCount = (await db.collection('awards').where({ username: targetUsername }).count()).total;

      return { code: 0, data: { summary, awardCount } };
    }

    // ========== 更新个人资料 ==========
    if (action === 'updateProfile') {
      const update = {};
      const allowedFields = ['name', 'weight', 'avatar'];
      for (const key of allowedFields) {
        if (event[key] !== undefined) {
          update[key] = event[key];
        }
      }
      if (!Object.keys(update).length) {
        return { code: 1, message: '没有需要更新的内容' };
      }
      update.updateTime = Date.now();

      const userCol = db.collection('users');
      const userQ = await userCol.where({ username }).get();
      if (!userQ.data.length) {
        return { code: 1, message: '用户不存在' };
      }
      const user = userQ.data[0];
      await userCol.doc(user._id).update({ data: update });
      const updated = (await userCol.doc(user._id).get()).data;
      return { code: 0, data: updated };
    }

    // ========== 修改密码 ==========
    if (action === 'changePassword') {
      const { oldPassword, newPassword } = event;
      if (!oldPassword || !newPassword) {
        return { code: 1, message: '密码不能为空' };
      }
      if (newPassword.length < 4) {
        return { code: 1, message: '新密码至少4位' };
      }

      const userCol = db.collection('users');
      const userQ = await userCol.where({ username }).get();
      if (!userQ.data.length) return { code: 1, message: '用户不存在' };
      const user = userQ.data[0];

      if (user.password !== oldPassword) {
        return { code: 1, message: '原密码错误' };
      }

      await userCol.doc(user._id).update({
        data: { password: newPassword, updateTime: Date.now() }
      });
      return { code: 0, data: { ok: true } };
    }

    // ========== 全表去重（openid 迁移遗留数据）==========
    if (action === 'dedupUsers') {
      const LIMIT = 100;
      let removed = 0;
      const all = await db.collection('users').orderBy('createTime', 'asc').limit(LIMIT).get();
      const byUsername = {};
      all.data.forEach(u => {
        if (!u.username) return;
        (byUsername[u.username] = byUsername[u.username] || []).push(u);
      });
      for (const ids of Object.values(byUsername)) {
        if (ids.length < 2) continue;
        const keep = ids[0];
        const mergeFields = {};
        ids.slice(1).forEach(other => {
          Object.keys(other).forEach(k => {
            if (k === '_id' || k === '_openid') return;
            if (keep[k] === undefined && other[k] !== undefined && other[k] !== '' && !(Array.isArray(other[k]) && other[k].length === 0)) {
              mergeFields[k] = other[k];
            }
          });
        });
        for (let i = 1; i < ids.length; i++) {
          await db.collection('users').doc(ids[i]._id).remove();
          removed++;
        }
        if (Object.keys(mergeFields).length) {
          await db.collection('users').doc(keep._id).update({ data: mergeFields });
        }
      }
      return { code: 0, data: { processed: all.data.length, removed } };
    }

    // ========== 上传头像 ==========
    if (action === 'uploadAvatar') {
      const { fileContent, cloudPath } = event;
      if (!fileContent || !cloudPath) {
        return { code: 1, message: '缺少参数' };
      }

      let fileBuffer;
      if (fileContent.startsWith('data:')) {
        const base64Data = fileContent.replace(/^data:image\/\w+;base64,/, '');
        fileBuffer = Buffer.from(base64Data, 'base64');
      } else {
        fileBuffer = Buffer.from(fileContent, 'base64');
      }

      if (fileBuffer.length > 1024 * 1024) {
        return { code: 1, message: '文件过大，最大支持 1MB' };
      }

      const uploadResult = await cloud.uploadFile({
        cloudPath,
        fileContent: fileBuffer,
      });

      const tempUrlResult = await cloud.getTempFileURL({
        fileList: [uploadResult.fileID],
      });

      const url = tempUrlResult.fileList[0]?.tempFileURL || uploadResult.fileID;
      return { code: 0, data: { fileID: uploadResult.fileID, url } };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('login cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};
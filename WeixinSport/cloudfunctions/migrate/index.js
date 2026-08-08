// cloudfunctions/migrate/index.js
// 数据迁移脚本：将 openid 关联改为 username 关联
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

exports.main = async (event, context) => {
  const action = event.action || 'run';

  try {
    // 1. 构建 openid -> username 映射
    console.log('[migrate] 开始构建 openid -> username 映射...');
    const userQ = await db.collection('users').get();
    const openidToUsername = {};
    userQ.data.forEach(u => {
      if (u.openid && u.username) {
        openidToUsername[u.openid] = u.username;
      }
    });
    console.log('[migrate] 映射构建完成:', Object.keys(openidToUsername).length, '条');

    if (action === 'run' || action === 'checkins') {
      // 2. 迁移 checkins 表
      console.log('[migrate] 开始迁移 checkins 表...');
      const checkinQ = await db.collection('checkins').get();
      let checkinsMigrated = 0;
      for (const c of checkinQ.data) {
        if (!c.username && c.openid && openidToUsername[c.openid]) {
          const username = openidToUsername[c.openid];
          await db.collection('checkins').doc(c._id).update({
            data: { username }
          });
          checkinsMigrated++;
        }
      }
      console.log('[migrate] checkins 迁移完成:', checkinsMigrated, '条');
    }

    if (action === 'run' || action === 'class_members') {
      // 3. 迁移 class_members 表
      console.log('[migrate] 开始迁移 class_members 表...');
      const memberQ = await db.collection('class_members').get();
      let membersMigrated = 0;
      for (const m of memberQ.data) {
        if (!m.username && m.openid && openidToUsername[m.openid]) {
          const username = openidToUsername[m.openid];
          await db.collection('class_members').doc(m._id).update({
            data: { username }
          });
          membersMigrated++;
        }
      }
      console.log('[migrate] class_members 迁移完成:', membersMigrated, '条');
    }

    if (action === 'run' || action === 'class_teachers') {
      // 4. 迁移 class_teachers 表
      console.log('[migrate] 开始迁移 class_teachers 表...');
      const teacherQ = await db.collection('class_teachers').get();
      let teachersMigrated = 0;
      for (const t of teacherQ.data) {
        if (!t.username && t.openid && openidToUsername[t.openid]) {
          const username = openidToUsername[t.openid];
          await db.collection('class_teachers').doc(t._id).update({
            data: { username }
          });
          teachersMigrated++;
        }
      }
      console.log('[migrate] class_teachers 迁移完成:', teachersMigrated, '条');
    }

    if (action === 'run' || action === 'awards') {
      // 5. 迁移 awards 表
      console.log('[migrate] 开始迁移 awards 表...');
      const awardQ = await db.collection('awards').get();
      let awardsMigrated = 0;
      for (const a of awardQ.data) {
        if (!a.username && a.openid && openidToUsername[a.openid]) {
          const username = openidToUsername[a.openid];
          await db.collection('awards').doc(a._id).update({
            data: { username }
          });
          awardsMigrated++;
        }
      }
      console.log('[migrate] awards 迁移完成:', awardsMigrated, '条');
    }

    // 6. 迁移 classes 表中的 teacherOpenid -> creatorUsername
    if (action === 'run' || action === 'classes') {
      console.log('[migrate] 开始迁移 classes 表...');
      const classQ = await db.collection('classes').get();
      let classesMigrated = 0;
      for (const c of classQ.data) {
        if (!c.creatorUsername && c.teacherOpenid && openidToUsername[c.teacherOpenid]) {
          const username = openidToUsername[c.teacherOpenid];
          await db.collection('classes').doc(c._id).update({
            data: { creatorUsername: username }
          });
          classesMigrated++;
        }
      }
      console.log('[migrate] classes 迁移完成:', classesMigrated, '条');
    }

    // 7. 迁移 users 表中的 childOpenid -> childUsername
    if (action === 'run' || action === 'users') {
      console.log('[migrate] 开始迁移 users 表...');
      let usersMigrated = 0;
      for (const u of userQ.data) {
        const updates = {};
        
        // childOpenid -> childUsername
        if (u.childOpenid && openidToUsername[u.childOpenid]) {
          updates.childUsername = openidToUsername[u.childOpenid];
        }
        
        // parentOpenids -> parentUsernames
        if (u.parentOpenids && u.parentOpenids.length) {
          const parentUsernames = u.parentOpenids
            .map(oid => openidToUsername[oid])
            .filter(Boolean);
          if (parentUsernames.length > 0) {
            updates.parentUsernames = parentUsernames;
          }
        }
        
        if (Object.keys(updates).length > 0) {
          await db.collection('users').doc(u._id).update({ data: updates });
          usersMigrated++;
        }
      }
      console.log('[migrate] users 迁移完成:', usersMigrated, '条');
    }

    return { 
      code: 0, 
      data: { 
        message: '迁移完成',
        mappingCount: Object.keys(openidToUsername).length
      }
    };
  } catch (e) {
    console.error('migrate error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};

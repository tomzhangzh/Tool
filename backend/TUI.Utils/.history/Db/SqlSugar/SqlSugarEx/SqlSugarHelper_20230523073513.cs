// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.DatabaseAccessor;
using System.Data;

namespace Abc.Utils;

/*
 * 单例模式
 https://www.donet5.com/Home/Doc?typeId=2352
 */

/// <summary>
/// sqlsugar 单例模式，可继承，可静态方式使用
/// </summary>
public static class SqlSugarHelper //不能是泛型类
{
    //  //用单例模式
    //  public static SqlSugarScope Db = new SqlSugarScope(SqlSugarCore.GetConnectionConfig(),
    //db => db.SqlSugarConfig());

    //用单例模式
    public static SqlSugarScope Db = new SqlSugarScope(
         SqlSugarHelper.SqlSugarDbSetting.ConnectionConfigs
  , db => db.InitDbAop()

  );

    /// <summary>
    /// 获取数据库连接配置
    /// </summary>
    /// <returns></returns>
    public static List<ConnectionConfig> GetConnectionConfig()
    {
        return SqlSugarDbSetting.ConnectionConfigs;
    }

    /// <summary>
    /// 缓存 sqlsugar 数据库配置 选项
    /// </summary>
    public static SqlSugarDbSettingOptions SqlSugarDbSetting => GetSqlSugarDbSettingOptions();

    /// <summary>
    /// 监听方法
    /// </summary>
    public static ConcurrentDictionary<Type, ConcurrentDictionary<Type, MethodInfo>> ListenerMethodInfos = new();

    /// <summary>
    /// 初始化
    /// </summary>
    public static void Init()
    {
        var dics = new Dictionary<Type, string>();
        dics.Add(typeof(ISqlSugarDiffLogListener), "DiffLog");//差异日志
        dics.Add(typeof(ISqlSugarDataChangedListener), "OnChanged");//增、删、查 和 改 事件
        dics.Add(typeof(ISqlSugarAopFilter), "Filter");//AOP 全局过滤器
        //使用
        //var instance = Activator.CreateInstance(app);
        //method.Invoke(instance, new object[] { dbProvider, diffLogModel });

        {
            foreach (var item in dics)
            {
                var methods = new ConcurrentDictionary<Type, MethodInfo>();

                //item.Key  是type 例如：ISqlSugarDiffLogListener
                //item.value 是 方法名称，例如： "DiffLog";

                var apps = App.EffectiveTypes.Where(u => u.GetInterfaces()
                      .Any(i => i.HasImplementedRawGeneric(item.Key)));
                foreach (var app in apps)
                {
                    var method = app.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                                       .Where(u => u.Name == item.Value
                                                                           && u.GetParameters().Length > 0)
                                                                       .FirstOrDefault();
                    if (method == null) continue;
                    methods.TryAdd(app, method);
                }
                //加入到监听集合中去
                ListenerMethodInfos.TryAdd(item.Key, methods);
            }
        }
    }

    /// <summary>
    /// 根据 Interface 的type 获取监听方法
    /// </summary>
    /// <param name="type">Interface 的type </param>
    /// <returns></returns>
    public static ConcurrentDictionary<Type, MethodInfo> GetMethods(Type type)
    {
        if (ListenerMethodInfos.TryGetValue(type, out var methods))
        {
            return methods;
        }
        else
        {
            return new ConcurrentDictionary<Type, MethodInfo>();
        }
    }

    #region 获取数据库连接配置，AOP

    /// <summary>
    /// 获取 SqlSugarDbSetting Options
    /// </summary>
    /// <returns></returns>
    public static SqlSugarDbSettingOptions GetSqlSugarDbSettingOptions()
    {
        //if (SqlSugarDbSetting != null) return SqlSugarDbSetting;
        var sqlSugarDbSetting = AppEx.GetConfig<SqlSugarDbSettingOptions>();
        foreach (var conn in sqlSugarDbSetting.ConnectionConfigs)
        {
            conn.ConfigureExternalServices = EntitySetting(sqlSugarDbSetting, conn);
            conn.MoreSettings = new ConnMoreSettings()
            {
                //所有 增、删 、改 会自动调用.RemoveDataCache()
                //意思就是 增、删 、改 都会移除对应表数据的 sqlsugar缓存
                IsAutoRemoveDataCache = true,

                //MySql禁用NVarchar
                DisableNvarchar = conn.DbType == SqlSugar.DbType.MySql ? true : false,
                SqlServerCodeFirstNvarchar = true,//SqlServer特殊配置：和他库不同一般选用Nvarchar，可以使用这个配置让他和其他数据库区分（其他库是varchar）
            };
        }
        //SqlSugarDbSetting = sqlSugarDbSetting;
        return sqlSugarDbSetting;
    }

    /// <summary>
    /// 配置实体
    /// </summary>
    /// <returns></returns>
    public static ConfigureExternalServices EntitySetting(SqlSugarDbSettingOptions sqlSugarDbSetting, ConnectionConfig conn)
    {
        ICacheService sqlSugarCache = new SqlSugarDistributedCache();
        return new ConfigureExternalServices
        {
            DataInfoCacheService = sqlSugarCache, //配置我们创建的缓存类，具体用法看标题5
            EntityService = (property, column) =>
            {
                var attributes = property.GetCustomAttributes(true);//get all attributes

                var sugarColumnAttribute = attributes.FirstOrDefault(o => o is SugarColumn);
                if (sugarColumnAttribute != null)
                {
                    //这里是sqlsugar的设置。。暂时没有。。
                }
                else
                {
                    //设置，标记了 Key 属性的字段为主键
                    var keyAttribute = attributes.FirstOrDefault(o => o is KeyAttribute);
                    if (keyAttribute != null)
                    {
                        column.IsPrimarykey = true; //有哪些特性可以看 1.2 特性明细
                    }
                }

                ////数据库没有进行初始化的时候才设置为自增
                ////必须是ID字段才执行，中间表可能会设置为自增，出现问题
                ////【 因为使用了 IsPrimarykey 所以得放到上面的代码后面 】
                //if (column.DbColumnName != null && column.DbColumnName.ToUpper() == "ID"
                //&& column.IsPrimarykey == true && column.IsIdentity == false
                ////&& sqlSugarDbSetting.EnableInitDatabase == false
                //&& (sqlSugarDbSetting.NoIsIdentityDbTables == null || !sqlSugarDbSetting.NoIsIdentityDbTables.All(a => a.ToLower() != column.DbTableName.ToLower()))
                //)
                //{
                //    //设置自增
                //    column.IsIdentity = sqlSugarDbSetting.EntityIsIdentity;
                //}

                var dbOptions = AppEx.GetConfig<SqlSugarDbSettingOptions>();

                #region ColumnAttribute

                {
                    //判断是否贴了 Column 属性
                    if (attributes.Any(o => o is ColumnAttribute))
                    {
                        var attribute = (ColumnAttribute)attributes.First(o => o is ColumnAttribute);

                        if (string.IsNullOrWhiteSpace(column.DataType) && !string.IsNullOrWhiteSpace(attribute.TypeName))
                        {
                            //设置自定义的TypeName
                            //[Column(TypeName = "decimal(18,2)")]
                            //public decimal Payment { get; set; }
                            column.DataType = attribute.TypeName;
                        }
                        if (string.IsNullOrWhiteSpace(column.OldDbColumnName) && !string.IsNullOrWhiteSpace(attribute.Name))
                        {
                            //修改列名用，这样不会新增或者删除列
                            column.OldDbColumnName = attribute.Name;
                        }
                    }
                }

                #endregion ColumnAttribute

                #region CommentAttribute

                {
                    if (attributes.Any(o => o is CommentAttribute))
                    {
                        var attribute = (CommentAttribute)attributes.First(o => o is CommentAttribute);
                        if (string.IsNullOrWhiteSpace(column.ColumnDescription) && !string.IsNullOrWhiteSpace(attribute.Comment))
                        {
                            column.ColumnDescription = attribute.Comment;
                        }
                    }
                }

                #endregion CommentAttribute

                #region Length

                {
                    if (attributes.Any(o => o is MaxLengthAttribute))
                    {
                        //判断是否设置了字符串的 MaxLength  自动设置
                        var attribute = (MaxLengthAttribute)attributes.First(o => o is MaxLengthAttribute);
                        //设置了 DataType格式的情况 length不要设置
                        if (string.IsNullOrWhiteSpace(column.DataType) && column.Length <= 0 && attribute.Length > 0)
                        {
                            column.Length = attribute.Length;
                        }
                    }
                }

                #endregion Length

                #region 字符串默认设置

                if (column.PropertyInfo.PropertyType.Name == nameof(System.String))
                {
                    if (string.IsNullOrWhiteSpace(column.DataType) && column.Length <= 0)
                    {
                        //没有设置 DataType，且字符串长度也没设置的情况下，默认使用大文本
                        column.DataType = StaticConfig.CodeFirst_BigString;
                    }

                    //默认都把字符串设置为可空,设置为false的话，根据实际配置来
                    if (dbOptions.StringIsNullable)
                    {
                        column.IsNullable = dbOptions.StringIsNullable;
                    }
                }

                #endregion 字符串默认设置

                #region 是否设置为自增

                {
                    //主键必须是是 int、long类型
                    //主键
                    if ((column.PropertyInfo.PropertyType.Name == nameof(System.Int32)
                    || column.PropertyInfo.PropertyType.Name == nameof(System.Int64))
                    && column.IsPrimarykey == false
                    && dbOptions.EntityIsIdentity
                    && !attributes.Any(o => o is SugarNoIdentityAttribute)//没有贴不自增的标签
                    )
                    {
                        var tablenames = new List<string>() { column.DbTableName };
                        if (dbOptions.EnableDbColumnNameToUnderLine)
                        {
                            var newtablename = UtilMethods.ToUnderLine(column.DbTableName);
                            tablenames.Add(newtablename);
                        }
                        //是否不自增，默认false
                        var isNoIdentity = false;
                        foreach (var tablename in dbOptions.NoIsIdentityDbTables)
                        {
                            if (tablenames.Any(o => o.ToLower() == tablename.ToLower()))
                            {
                                isNoIdentity = true;
                                break;
                            }
                        }
                        //没有被排除，可以设置为自增
                        if (isNoIdentity == false)
                        {
                            column.IsIdentity = true;
                        }
                    }
                }

                #endregion 是否设置为自增

                //else if (string.IsNullOrWhiteSpace(column.DataType))
                //{
                //    //string  大文本
                //    column.DataType = StaticConfig.CodeFirst_BigString;
                //}

                //// int?  decimal?这种 isnullable=true 不支持string(下面.NET 7支持)
                //if (column.IsPrimarykey == false && column.IsNullable == false && property.PropertyType.IsGenericType &&
                //property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                //{
                //    column.IsNullable = true;
                //}
                /***高版C#写法***/
                //int?  decimal?这种 isnullable=true  支持string?和string
                if (column.IsPrimarykey == false && column.IsNullable == false && new NullabilityInfoContext()
                  .Create(property).WriteState is NullabilityState.Nullable)
                {
                    column.IsNullable = true;
                }

                //如果是mysql的情况下，定义了 [SugarColumn(ColumnDataType = "varchar(max)")]，
                //此设置只支持sqlserver（max mysql不支持）兼容mysql 则修改为  longtext
                if (conn.DbType == SqlSugar.DbType.MySql && column.DataType == "varchar(max)")
                {
                    column.DataType = "longtext";
                }

                //ToUnderLine驼峰转下划线方法
                if (dbOptions.EnableDbColumnNameToUnderLine)
                {
                    column.DbColumnName = UtilMethods.ToUnderLine(column.DbColumnName);
                    column.OldDbColumnName = UtilMethods.ToUnderLine(column.OldDbColumnName);
                }
            },
            //这个是设置数据库表的名称的，例如，实体叫“User” 数据库叫“Sys_User”
            //解析了 EF的 Table 属性
            EntityNameService = (type, entity) =>
            {
                var attributes = type.GetCustomAttributes(true);
                if (attributes.Any(it => it is TableAttribute))
                {
                    var attr = attributes.FirstOrDefault(it => it is TableAttribute);
                    if (attr != null)
                    {
                        var tableAttribute = (TableAttribute)attr;
                        entity.DbTableName = tableAttribute.Name;
                    }

                    //entity.DbTableName = (attributes.First(it => it is TableAttribute) as TableAttribute).Name;
                }
                var dbOptions = AppEx.GetConfig<SqlSugarDbSettingOptions>();
                //ToUnderLine驼峰转下划线方法
                if (dbOptions.EnableDbTableNameToUnderLine)
                {
                    entity.DbTableName = ToUnderLine(entity.DbTableName);
                }
            }
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="str"></param>
    /// <param name="isToUpper"></param>
    /// <param name="isSkipUnderLine">是否跳过有下划线的</param>
    /// <returns></returns>
    public static string ToUnderLine(string str, bool isToUpper = false, bool isSkipUnderLine = false)
    {
        if ((str?.Contains("_") ?? true) && isSkipUnderLine)
        {
            return str;
        }
        var newTableName = "";
        if (isToUpper)
        {
            newTableName = string.Concat(str.Select((char x, int i) => (i > 0 && char.IsUpper(x)) ? ("_" + x) : x.ToString())).ToUpper();
        }

        newTableName = string.Concat(str.Select((char x, int i) => (i > 0 && char.IsUpper(x)) ? ("_" + x) : x.ToString())).ToLower();

        newTableName = newTableName.Replace("__", "_");
        return newTableName;
    }

    #endregion 获取数据库连接配置，AOP

    #region 缓存

    /// <summary>
    /// 删除SqlSugar 缓存
    /// </summary>
    /// <typeparam name="TEntity">数据库实体名</typeparam>
    public static void RemoveCacheByContainEntityName<TEntity>()
    {
        SqlSugarHelper.Db.RemoveCacheByContainEntityName<TEntity>();
    }

    /// <summary>
    /// 删除包含指定支付查的 SqlSugar 缓存
    /// </summary>
    /// <param name="likeString">相似字符串</param>
    public static void RemoveCacheByContainStr(string likeString)
    {
        SqlSugarHelper.Db.RemoveCacheByContainStr(likeString);
    }

    #region 刷新表缓存异步

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <returns></returns>
    public static async Task<List<TEntity>> RefreshCacheByTableAsync<TEntity>()
    {
        var entities = await SqlSugarHelper.Db.RefreshTableCacheByEntityAsync<TEntity>();
        return entities;
    }

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <param name="entityName">实体名称</param>
    /// <returns></returns>
    public static async Task<List<object>> RefreshCacheByTableAsync(string entityName)
    {
        var entities = await SqlSugarHelper.Db.RefreshTableCacheByEntityNameAsync(entityName);
        return entities;
    }

    #endregion 刷新表缓存异步

    #region 刷新表缓存同步

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <returns></returns>
    public static List<TEntity> RefreshCacheByTable<TEntity>()
    {
        var entities = SqlSugarHelper.Db.RefreshTableCacheByEntity<TEntity>();
        return entities;
    }

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <param name="entityName">实体名称</param>
    /// <returns></returns>
    public static List<object> RefreshCacheByTable(string entityName)
    {
        var entities = SqlSugarHelper.Db.RefreshTableCacheByEntityName(entityName);
        return entities;
    }

    #endregion 刷新表缓存同步

    #endregion 缓存

    #region 数据库初始化/数据种子

    public static void InitDb()
    {
        var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;

        //判断是否是初始化且启用了初始化数据库
        if (sqlSugarDbSetting.EnableInitDatabase == false) return;

        InitTables(new SqlSugarDbTableConfig());

    }
    /// <summary>
    /// 创建数据库
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns></returns>
    public static string CreateDb(SqlSugarDbTableConfig config)
    {
        var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;

        var resultMsg = "";
        try
        {
            if (string.IsNullOrWhiteSpace(config.DatabaseDirectory) && !string.IsNullOrWhiteSpace(sqlSugarDbSetting.DatabaseDirectory))
            {
                config.DatabaseDirectory = sqlSugarDbSetting.DatabaseDirectory;
            }

            //创建数据库
            //注意 ：Oracle和个别国产库需不支持该方法，需要手动建库 
            var _db = SqlSugarHelper.Db;
            foreach (var conn in sqlSugarDbSetting.ConnectionConfigs)
            {
                if (conn.DbType != SqlSugar.DbType.Oracle)
                    _db.DbMaintenance.CreateDatabase(config.DatabaseName, config.DatabaseDirectory);
            }

            resultMsg = "创建成功";
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\创建数据库");

            resultMsg = ex.Message;
        }
        return resultMsg;
    }

    /// <summary>
    /// 创建表
    /// </summary>
    /// <param name="config">数据库配置</param>
    public static void InitTables(SqlSugarDbTableConfig config)
    {
        try
        {
            var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;
            ///创建数据库
            CreateDb(config);

            //获取所有实体的 Type
            var entityTypes = GetAllEntitiesTypes();
            ////创建表、迁移表
            //SqlSugarDbContext.Db.ScopedContext.CodeFirst.InitTables(alltypes.ToArray());//根据types创建表

            var _db = SqlSugarHelper.Db;
            foreach (var entityType in entityTypes)
            {
                try
                {
                    //判断是否启用了租户，没有启用，则不生成租户配置表
                    if (sqlSugarDbSetting.EnableTanant == false && entityType.Name == "SysTenantDbConfig")
                    {
                        continue;
                    }
                    //是否贴了 租户属性，如果贴了取出贴了的 ConfigId
                    var tAtt = entityType.GetCustomAttribute<TenantAttribute>();
                    var configId = tAtt?.configId.ToString() ?? "";
                    if (configId == null || string.IsNullOrWhiteSpace(configId))
                    {
                        configId = sqlSugarDbSetting.ConnectionConfigs.FirstOrDefault()?.ConfigId;
                    }
                    if (string.IsNullOrWhiteSpace(configId))
                    {
                        configId = SqlSugarConst.ConfigId;
                    }
                    //var provider = _db.GetConnectionScope(tAtt == null ? SqlSugarConst.ConfigId : tAtt.configId);
                    var provider = _db.GetConnectionScope(configId);
                    //provider.CodeFirst.SetStringDefaultLength(0);//设置字符串默认长度
                    provider.CodeFirst
                        .SplitTables()
                        .InitTables(entityType);
                }
                catch (Exception ex)
                {
                    LogEx.Error($"表：{entityType.FullName}\r\n{ex.ToStringEx()}", "SqlSugar\\初始化数据库和表");
                }
            }
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\初始化数据库和表");
        }
    }
    /// <summary>
    /// 获取DB更加sqlsugar数据库配置
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns></returns>
    public static SqlSugarClient GetDbBySqlSugarDbTableConfig(SqlSugarDbTableConfig config)
    {
        try
        {
            if (config != null && !string.IsNullOrWhiteSpace(config.DbConn))
            {
                SqlSugar.ConnectionConfig conn = new ConnectionConfig();
                conn.DbType = config.DbType;
                conn.ConnectionString = config.DbConn;
                conn.IsAutoCloseConnection = true;
                var db = new SqlSugarClient(conn);
                db.SetAop();

                return db;
            }
            else
            {
                // throw new Exception("数据库字符串连接配置错误");
                return SqlSugarHelper.Db.ToClient();
            }
        }
        catch (Exception ex)
        {
            throw;
        }

    }

    ///// <summary>
    ///// 初始化数据种子委托方法，默认内置，可自定义
    ///// </summary>
    //public static Func<ISqlSugarClient, bool, bool> InitDataSeedFunc { get; set; }

    /// <summary>
    /// 初始化数据种子委托方法扩展，执行完数据种子方法后调用，处理一些特殊扩展
    /// </summary>
    public static Func<ISqlSugarClient, bool> InitDataSeedExFunc { get; set; }

    /// <summary>
    /// 初始化数据种子
    /// </summary>
    /// <returns></returns>
    public static bool InitDataSeed()
    {
        var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;
        //判断是否是初始化且启用了初始化数据库
        if (sqlSugarDbSetting.EnableInitDatabase == false) return false;

        return UpdateDataSeed();
    }
    /// <summary>
    /// 更新数据种子
    /// </summary>
    public static bool UpdateDataSeed(SqlSugarDbTableConfig config = null)
    {
        if (config != null)
        {
            ISqlSugarClient db = GetDbBySqlSugarDbTableConfig(config);

            return UpdateDataSeed(db.ToScope());
        }
        else
        {
            return UpdateDataSeed(SqlSugarHelper.Db);
        }
    }
    /// <summary>
    /// 更新数据种子
    /// </summary>
    public static bool UpdateDataSeed(SqlSugarScope db)
    {
        try
        {
            var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;


            // 获取所有实体种子数据
            var seedDataTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass
                && u.GetInterfaces().Any(i => i.HasImplementedRawGeneric(typeof(ISqlSugarEntitySeedData<>))));
            if (!seedDataTypes.Any()) return false;

            //循环处理数据种子
            foreach (var seedType in seedDataTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(seedType);
                    //获取数据种子的方法
                    var hasDataMethod = seedType.GetMethod("HasData", genericParameterCount: 0, new Type[] { });
                    //获取数据种子的数据
                    var seedData = ((IEnumerable)hasDataMethod?.Invoke(instance, null))?.Cast<object>();
                    if (seedData == null) continue;

                    var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                    var tAtt = entityType.GetCustomAttribute<TenantAttribute>();
                    //配置ID
                    var configId = tAtt?.configId.ToString() ?? "";
                    if (configId == null || string.IsNullOrWhiteSpace(configId))
                    {
                        configId = sqlSugarDbSetting.ConnectionConfigs.FirstOrDefault()?.ConfigId;
                    }
                    if (string.IsNullOrWhiteSpace(configId))
                    {
                        configId = SqlSugarConst.ConfigId;
                    }

                    //获取指定配置的连接
                    var provider = db.GetConnectionScope(configId);

                    var entityInfo = provider.EntityMaintenance.GetEntityInfo(entityType);

                    // var primarykeys = string.Join(',', entityInfo.Columns.Where(o => o.IsPrimarykey).Select(o => o.DbColumnName));

                    //var aa = provider.EntityMaintenance.GetDbColumnName("ExtensionData", entityType);

                    //手动构造 Where构造模式
                    var conModels = new List<IConditionalModel>();

                    foreach (object item in seedData)
                    {
                        //清空
                        conModels.Clear();
                        //获取数据的dictionary
                        var dics = item.GetDictionary();
                        foreach (var entityColumnInfo in entityInfo.Columns.Where(o => o.IsPrimarykey))
                        {
                            var state = dics.TryGetValue(entityColumnInfo.PropertyName, out string value);
                            if (state && !string.IsNullOrWhiteSpace(value))
                            {
                                conModels.Add(new ConditionalModel { FieldName = entityColumnInfo.DbColumnName, CSharpTypeName = entityColumnInfo.PropertyInfo.PropertyType.Name, ConditionalType = ConditionalType.Equal, FieldValue = value });
                            }
                        }
                        //有条件才进行，否则可能会出错
                        if (conModels.Any())
                        {
                            //数据库是否存在
                            var isExist = db.Queryable<object>().AS(entityInfo.DbTableName).Where(conModels).Any();
                            if (isExist)
                            {
                                var result2 = db.UpdateableByObject(item).ExecuteCommand();
                            }
                            else
                            {
                                db.InsertableByObject(item).ExecuteCommand();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogEx.Error($"表：{seedType.FullName}\r\n{ex.ToStringEx()}", "SqlSugar\\初始化数据种子");
                }
            }

            // 初始化数据种子委托方法扩展，执行完数据种子方法后调用，处理一些特殊扩展
            if (InitDataSeedExFunc != null)
            {
                return InitDataSeedExFunc(db);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\初始化数据种子");
            return false;
        }
    }

    /// <summary>
    /// 初始化数据种子
    /// </summary>
    private static bool InitDataSeed_old(bool isInit = false)
    {
        try
        {
            var db = SqlSugarHelper.Db;

            var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;
            //判断是否是初始化且启用了初始化数据库
            if (sqlSugarDbSetting.EnableInitDatabase == false && isInit == true) return false;

            //判断是否是初始化且启用了初始化数据种子
            if (sqlSugarDbSetting.EnableInitDataSeed == false && isInit == true) return false;

            // 获取所有实体种子数据
            var seedDataTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass
                && u.GetInterfaces().Any(i => i.HasImplementedRawGeneric(typeof(ISqlSugarEntitySeedData<>))));
            if (!seedDataTypes.Any()) return false;

            //循环处理数据种子
            foreach (var seedType in seedDataTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(seedType);
                    //获取数据种子的方法
                    var hasDataMethod = seedType.GetMethod("HasData", genericParameterCount: 0, new Type[] { });
                    //获取数据种子的数据
                    var seedData = ((IEnumerable)hasDataMethod?.Invoke(instance, null))?.Cast<object>();
                    if (seedData == null) continue;

                    var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                    var tAtt = entityType.GetCustomAttribute<TenantAttribute>();
                    //配置ID
                    var configId = tAtt?.configId.ToString() ?? "";
                    if (configId == null || string.IsNullOrWhiteSpace(configId))
                    {
                        configId = sqlSugarDbSetting.ConnectionConfigs.FirstOrDefault()?.ConfigId;
                    }
                    if (string.IsNullOrWhiteSpace(configId))
                    {
                        configId = SqlSugarConst.ConfigId;
                    }

                    //var provider = db.GetConnectionScope(tAtt == null ? SqlSugarConst.ConfigId : tAtt?.configId);
                    //获取指定配置的连接
                    var provider = db.GetConnectionScope(configId);

                    var seedDataTable = seedData.ToList().ToDataTable();
                    seedDataTable.TableName = db.EntityMaintenance.GetEntityInfo(entityType).DbTableName;
                    if (seedDataTable.Columns.Contains(SqlSugarConst.PrimaryKey))
                    {
                        var storage = provider.Storageable(seedDataTable).WhereColumns(SqlSugarConst.PrimaryKey).ToStorage();
                        // 如果添加一条种子数，sqlsugar 默认以 @param 的方式赋值，如果 PropertyType 为空，则默认数据类型为字符串。插入 pgsql 时候会报错，所以要忽略为空的值添加

                        var isSingle = ((InsertableProvider<Dictionary<string, object>>)storage.AsInsertable).IsSingle;
                        if (isSingle)
                        {
                            //var sql = storage.AsInsertable.IgnoreColumns("Id", "UpdateTime", "UpdateUserId", "CreatorUserId").ToSqlString();
                            storage.AsInsertable.ExecuteCommand();
                        }
                        else
                        {
                            var a = storage.AsInsertable.ToSqlString();
                            storage.AsInsertable.ExecuteCommand();
                        }
                        //_ = ((InsertableProvider<Dictionary<string, object>>)storage.AsInsertable).IsSingle ?
                        //    storage.AsInsertable.IgnoreColumns("UpdateTime", "UpdateUserId", "CreatorUserId", "Id").ExecuteCommand() :
                        //    storage.AsInsertable.IgnoreColumns("Id").ExecuteCommand();
                        storage.AsUpdateable.ExecuteCommand();
                    }
                    else // 没有主键或者不是预定义的主键(没主键有重复的可能)
                    {
                        var storage = provider.Storageable(seedDataTable).ToStorage();
                        storage.AsInsertable.ExecuteCommand();
                    }
                }
                catch (Exception ex)
                {
                    LogEx.Error($"表：{seedType.FullName}\r\n{ex.ToStringEx()}", "SqlSugar\\初始化数据种子");
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\初始化数据种子");
            return false;
        }
    }

    /// <summary>
    /// 数据库表 结构对比
    /// </summary>
    /// <returns>返回表结构对比结果</returns>
    public static string DifferenceTables(string dbType, string connStr)
    {
        var dt = dbType.ToEnum<SqlSugar.DbType>();
        return DifferenceTables(new SqlSugarDbTableConfig() { DbType = dt, DbConn = connStr });
    }

    /// <summary>
    /// 数据库表 结构对比
    /// </summary>
    /// <returns>返回表结构对比结果</returns>
    public static string DifferenceTables(SqlSugarDbTableConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DbConn))
        {
            //对比当前数据库
            return DifferenceTables();
        }
        else
        {
            var db = GetDbBySqlSugarDbTableConfig(config);
            return DifferenceTables(db);
        }

    }

    /// <summary>
    /// 数据库表 结构对比
    /// </summary>
    /// <returns>返回表结构对比结果</returns>
    public static string DifferenceTables(SqlSugar.ConnectionConfig conn)
    {
        conn.IsAutoCloseConnection = true;
        var db = new SqlSugarClient(conn);
        db.SetAop();//设置AOP
        return db.DifferenceTables();
    }

    /// <summary>
    /// 数据库表 结构对比
    /// </summary>
    /// <returns>返回表结构对比结果</returns>
    public static string DifferenceTables()
    {
        return SqlSugarHelper.Db.DifferenceTables();
    }

    /// <summary>
    /// 数据库表 结构对比
    /// </summary>
    /// <returns>返回表结构对比结果</returns>
    public static string DifferenceTables(this ISqlSugarClient _db)
    {
        if (_db == null)
        {
            _db = SqlSugarHelper.Db;
        }
        //获取所有实体的 Type
        var alltypes = GetAllEntitiesTypes();
        //对比表
        var diffString = _db.CodeFirst.GetDifferenceTables(alltypes.ToArray()).ToDiffString();
        //返回表的对比结果
        return diffString;
    }

    /// <summary>
    /// 获取所有实体的 Type
    /// </summary>
    /// <param name="isCompatibleEF">是否兼容EF</param>
    /// <returns></returns>
    public static Type[] GetAllEntitiesTypes(bool isCompatibleEF = true)
    {
        if (isCompatibleEF)
        {
            var types = App.EffectiveTypes.Where(u =>
                !u.IsInterface
                && !u.IsAbstract
                && u.IsClass
                && (u.IsDefined(typeof(SugarTable), false) || u.IsDefined(typeof(TableAttribute), false))
                && !u.IsDefined(typeof(NotTableAttribute), false)
               ).ToArray();

            return types;
        }
        else
        {
            var types = App.EffectiveTypes.Where(u =>
                !u.IsInterface
                && !u.IsAbstract
                && u.IsClass
                && u.IsDefined(typeof(SugarTable), false)
                && !u.IsDefined(typeof(NotTableAttribute), false)
               ).ToArray();

            return types;
        }
    }

    #endregion 数据库初始化/数据种子

    #region Comm



    /// <summary>
    /// SqlSugar获取实体信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugar.EntityInfo GetEntityInfoByAllDb<T>(ISqlSugarClient db = null)
    {
        return GetEntityInfoByAllDb(typeof(T), db);
    }

    /// <summary>
    /// SqlSugar获取实体信息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugar.EntityInfo GetEntityInfoByAllDb(Type type, ISqlSugarClient db = null)
    {
        foreach (var conn in GetConnectionConfig())
        {
            var db2 = db.ToTenant((string)conn.ConfigId);
            var entityInfo = db2.EntityMaintenance.GetEntityInfo(type);
            if (entityInfo != null)
            {
                return entityInfo;
            }
        }
        return null;
    }

    #endregion Comm
}
using GachaWebBackend.Model;
using PublicInfos;
using SqlSugar;
using System;
using System.Diagnostics;

namespace GachaWebBackend.Helper
{
    public static class SqlHelper
    {
        static string DBPath { get; set; } = Appsettings.app(new string[] { "DBPath" });
        private static SqlSugarClient GetInstance()
        {
            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = $"data source={DBPath}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
            });
        }
        /// <summary>
        /// 数据库不存在时将会创建
        /// </summary>
        public static void CreateDB()
        {
            using (var db = GetInstance())
            {
                //TODO: 插件发布时替换此处
                //db.DbMaintenance.CreateDatabase(DBPath);
                db.DbMaintenance.CreateDatabase(Path.Combine(Environment.CurrentDirectory, "data.db"));
                db.CodeFirst.InitTables(typeof(DB_Repo));
                db.CodeFirst.InitTables(typeof(DB_User));
                db.CodeFirst.InitTables(typeof(Pool));
                db.CodeFirst.InitTables(typeof(GachaItem));
                db.CodeFirst.InitTables(typeof(Config));
                db.CodeFirst.InitTables(typeof(OrderConfig));
                db.CodeFirst.InitTables(typeof(Category));
                db.CodeFirst.InitTables(typeof(ApiAuth));
                db.CodeFirst.InitTables(typeof(WebUser));
            }
        }
        #region ---WebUsers---
        public static void UpdateUser(this WebUser user)
        {
            using var db = GetInstance();
            db.Updateable(user).ExecuteCommand();
        }
        public static WebUser Login(string username, string password)
        {
            using var db = GetInstance();
            return db.Queryable<WebUser>().First(x => (x.Email == username || x.QQ.ToString() == username || x.Nickname == username) && x.Password == password);
        }
        public static WebUser GetUserByID(long QQ)
        {
            using var db = GetInstance();
            return db.Queryable<WebUser>().First(x => x.QQ == QQ);
        }
        public static bool Register(WebUser user)
        {
            using var db = GetInstance();
            user.Developer = 0;
            user.RegisterDate = DateTime.Now;
            user.Password = WebCommonHelper.MD5Encrypt(user.Password);
            try
            {
                if(db.Queryable<WebUser>().Any(x=> x.QQ == user.QQ || x.Email == user.Email))
                {
                    return false;
                }
                db.Insertable(user).ExecuteCommand();
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("[-] Register Error:" + e.Message);
                return false; 
            }
        }
        public static bool VerifyQQ(long QQ)
        {
            using var db = GetInstance();
            return !db.Queryable<WebUser>().Any(x => x.QQ == QQ);
        }
        public static bool VerifyEmail(string email)
        {
            using var db = GetInstance();
            return !db.Queryable<WebUser>().Any(x => x.Email == email);
        }
        public static void ResetPassword(string email, string newpassword)
        {
            using var db = GetInstance();
            var user = db.Queryable<WebUser>().First(x => x.Email == email);
            user.Password = newpassword;
            db.Updateable(user).ExecuteCommand();
        }
        #endregion

        #region ---Configs---
        public static void LoadConfig()
        {
            ConfigCache.UserConfigs.Clear();
            using var db = GetInstance();
            var c = db.Queryable<Config>().ToList();
            if (c.Count == 0)
            {
                var newConfig = new Config
                {
                    QQID = 0,
                    SignFloor = 1600,
                    SignCeil = 3200,
                    SignResetTime = new DateTime(1970, 1, 1, 0, 0, 0),
                    RegisterMoney = 6400,
                    GachaCost = 160
                };
                c.Add(newConfig);
                db.Insertable(c).ExecuteCommand();
            }
            foreach (var item in c)
            {
                ConfigCache.UserConfigs.Add(item.QQID, item);
            }

            var o = db.Queryable<OrderConfig>().ToList();
            if (o.Count == 0)
            {
                var newOrderConfig = new OrderConfig
                {
                    QQID = 0,
                    RegisterOrder = "#抽卡注册",
                    SignOrder = "#抽卡签到",
                    NonRegisterText = "尚未注册，输入 #抽卡注册 来进行一个册的注吧",
                    SuccessfulSignText = "<@>签到成功，获得通用货币<$0>",
                    DuplicateSignText = "<@>你今天签过到了",
                    SuccessfulRegisterText = "<@>注册成功，获得通用货币<current_money>",
                    DuplicateRegisterText = "<@>重复注册是打咩的",
                    LeakMoneyText = "<@>剩余货币不足以抽卡了呢~\n你目前还有<current_money>通用货币",
                };
                o.Add(newOrderConfig);
                db.Insertable(o).ExecuteCommand();
            }
            foreach (var item in o)
            {
                ConfigCache.UserOrderConfigs.Add(item.QQID, item);
            }
        }
        public static void UpdateConfig(Config config, long ApiKey = 0)
        {
            ConfigCache.UserConfigs[ApiKey] = config.Clone();
            using (var db = GetInstance())
            {
                db.Updateable(config).ExecuteCommand();
            }
        }
        public static void UpdateOrderConfig(OrderConfig config, long ApiKey = 0)
        {
            ConfigCache.UserOrderConfigs[ApiKey] = config.Clone();
            using (var db = GetInstance())
            {
                db.Updateable(config).ExecuteCommand();
            }
        }
        #endregion
    }
}

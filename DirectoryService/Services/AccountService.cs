using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TEUtil;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;

namespace DirectoryService.Services
{
    public class AccountService
    {
        IConfiguration _configuration;

        public string ConnectionString { get; set; }
        public bool IsLoggedIn { get; private set; }
        public string Username { get; set; }

        public AccountService(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnectionString = _configuration.GetConnectionString("Default");
        }

        private async Task InitializeDefaultAdmin()
        {
            using (ITEDirectoryDB db = DirectoryServiceUtil.GetDB(ConnectionString))
            {
                string defaultAdmin = _configuration.GetValue<string>("DefaultAdmin");
                ITEDirectoryAdmin admin = await db.LoadAdmin(defaultAdmin);
                if (admin == null)
                {
                    admin = new TEDirectoryAdmin();
                    admin.Email = defaultAdmin;
                    admin.EncryptedPassword = Encryption.Encrypt("changeme");
                    await db.SaveAdmin(admin);
                }
            }
        }

        /// <summary>
        ///  This method logs in the user. It returns a NULL string if it logs in successfully.
        /// </summary>
        public async Task<string> Login(string username, string password)
        {
            await InitializeDefaultAdmin();

            this.IsLoggedIn = false;
            using (ITEDirectoryDB db = DirectoryServiceUtil.GetDB(ConnectionString))
            {
                string encryptedPassword = Encryption.Encrypt(password);
                ITEDirectoryAdmin admin = await db.LoadAdmin(username);
                if (admin == null || admin.EncryptedPassword != encryptedPassword)
                {
                    return "Failed to login.";
                }
                else
                {
                    this.IsLoggedIn = true;
                    return null;
                }
            }
        }
    }
}

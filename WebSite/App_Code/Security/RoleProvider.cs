using System;
using System.Configuration;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System.Web;
using System.Web.Security;
using System.Collections;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Web.Hosting;
using System.Web.Caching;
using System.Web.Configuration;
using System.ComponentModel;

namespace Planned.Security
{
	public partial class ApplicationRoleProvider : ApplicationRoleProviderBase
    {
    }
    
    public class ApplicationRoleProviderBase : RoleProvider
    {
        
        private string _applicationName;
        
        private string _activeDirectoryConnectionString;
        
        private string _server;
        
        private string _username;
        
        private string _password;
        
        private bool _isRoleWhiteListMode;
        
        private ArrayList _roleWhiteList = new ArrayList();
        
        private ArrayList _roleBlackList = new ArrayList();
        
        private ArrayList _userBlackList = new ArrayList();
        
        private bool _enableCache;
        
        private int _cacheTimeoutInMinutes = 10;
        
        private string[] _defaultUserBlackList = new string[] {
                "Administrator",
                "TsInternetUser",
                "Guest",
                "krbtgt",
                "Replicate",
                "SERVICE",
                "SMSService"};
        
        private string[] _defaultRoleBlackList = new string[] {
                "Domain Guests",
                "Domain Computers",
                "Group Policy Creator Owners",
                "Guests",
                "Domain Users",
                "Pre-Windows 200 Compatible Access",
                "Exchange Domain Servers",
                "Schema Admins",
                "Enterprise Admins",
                "Domain Admins",
                "Cert Publishers",
                "Backup Operators",
                "WINS Users",
                "DnsAdmins",
                "DnsUpdateProxy",
                "DHCP Users",
                "DHCP Administrators",
                "Exchange Services",
                "Exchange Enterprise Servers",
                "Remote Desktop Users",
                "Network Configuration Operators",
                "Incoming Forest Trust Builders",
                "Performance Monitor Users",
                "Performance Log Users",
                "Windows Authorization Access Group",
                "Terminal Server License Servers",
                "Distributed COM Users",
                "MTS Impersonators",
                "Everyone",
                "LOCAL",
                "Authenticated Users"};
        
        private ContextType _aDContextType = ContextType.Machine;
        
        public virtual ArrayList RoleWhiteList
        {
            get
            {
                return _roleWhiteList;
            }
            set
            {
                _roleWhiteList = value;
            }
        }
        
        public virtual ArrayList RoleBlackList
        {
            get
            {
                return _roleBlackList;
            }
            set
            {
                _roleBlackList = value;
            }
        }
        
        public virtual ArrayList UserBlackList
        {
            get
            {
                return _userBlackList;
            }
            set
            {
                _userBlackList = value;
            }
        }
        
        public override string ApplicationName
        {
            get
            {
                return _applicationName;
            }
            set
            {
                _applicationName = value;
            }
        }
        
        public virtual ContextType ADContextType
        {
            get
            {
                return _aDContextType;
            }
            set
            {
                _aDContextType = value;
            }
        }
        
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            	throw new ArgumentNullException("config");
            if (String.IsNullOrEmpty(name))
            	name = "ADRoleProvider";
            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Active Directory Role Provider");
            }
            base.Initialize(name, config);
            string contextType = config["contextType"];
            if (!(String.IsNullOrEmpty(contextType)))
            	ADContextType = ((ContextType)(TypeDescriptor.GetConverter(typeof(ContextType)).ConvertFromString(contextType)));
            string temp = config["activeDirectoryConnectionString"];
            if (String.IsNullOrEmpty(temp))
            	throw new ProviderException("The attribute \'activeDirectoryConnectionString\' is missing or empty.");
            ConnectionStringSettings connObj = ConfigurationManager.ConnectionStrings[temp];
            if (connObj != null)
            	_activeDirectoryConnectionString = connObj.ConnectionString;
            if (String.IsNullOrEmpty(_activeDirectoryConnectionString))
            	throw new ProviderException("The connection name \'activeDirectoryConnectionString\' was not found in the applic" +
                        "ations configuration or the connection string is empty.");
            if (_activeDirectoryConnectionString.Substring(0, 7) == "LDAP://")
            {
                _server = _activeDirectoryConnectionString.Substring(7, (_activeDirectoryConnectionString.Length - 7));
                MembershipSection membershipSection = ((MembershipSection)(WebConfigurationManager.GetSection("system.web/membership")));
                string defaultProvider = membershipSection.DefaultProvider;
                ProviderSettings providerSettings = membershipSection.Providers[defaultProvider];
                _username = providerSettings.Parameters["connectionUsername"];
                _password = providerSettings.Parameters["connectionPassword"];
            }
            else
            	throw new ProviderException("The connection string specified in \'activeDirectoryConnectionString\' does not app" +
                        "ear to be a valid LDAP connection string.");
            _applicationName = config["applicationName"];
            if (String.IsNullOrEmpty(_applicationName))
            	_applicationName = HostingEnvironment.ApplicationVirtualPath;
            if (_applicationName.Length > 256)
            	throw new ProviderException("The application name is too long.");
            temp = config["roleWhitelist"];
            if (!(String.IsNullOrEmpty(temp)))
            	_isRoleWhiteListMode = true;
            else
            	_isRoleWhiteListMode = false;
            if (_isRoleWhiteListMode)
            {
                if (!(String.IsNullOrEmpty(config["roleWhitelist"])))
                	foreach (string group in config["roleWhitelist"].Trim().Split(','))
                    	_roleWhiteList.Add(group.Trim());
            }
            foreach (string group in _defaultRoleBlackList)
            	_roleBlackList.Add(group.Trim());
            if (!(String.IsNullOrEmpty(config["roleBlacklist"])))
            	foreach (string group in config["roleBlacklist"].Trim().Split(','))
                	_roleBlackList.Add(group.Trim());
            foreach (string user in _defaultUserBlackList)
            	_userBlackList.Add(user.Trim());
            if (!(String.IsNullOrEmpty(config["userBlacklist"])))
            	foreach (string user in config["userBlacklist"].Trim().Split(','))
                	_userBlackList.Add(user.Trim());
            if (!(String.IsNullOrEmpty(config["enableCache"])))
            	if (config["enableCache"] == "True")
                	_enableCache = true;
                else
                	if (config["enableCache"] == "False")
                    	_enableCache = false;
                    else
                    	throw new ProviderException("The attribute \'enableCache\' is specified as an invalid value. Must be \'True\' or \'" +
                                "False\'.");
            if (_enableCache)
            {
                temp = config["cacheTimeInMinutes"];
                try
                {
                    _cacheTimeoutInMinutes = Convert.ToInt32(temp);
                }
                finally
                {
                    // release resources here
                }
            }
            // This code is based on http://www.codeproject.com/Articles/28546/Active-Directory-Roles-Provider.
        }
        
        public override string[] GetRolesForUser(string username)
        {
            if (_enableCache)
            {
                string CachedValue;
                CachedValue = GetCacheItem('U', username);
                if (CachedValue != "*NotCached")
                	return CachedValue.Split(',');
            }
            ArrayList results = new ArrayList();
            using (PrincipalContext context = new PrincipalContext(ADContextType, _server, _username, _password))
            	try
                {
                    UserPrincipal p = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                    EnumerateGroups(p.GetGroups(), results);
                }
                catch (Exception ex)
                {
                    throw new ProviderException("Unable to query Active Directory.", ex);
                }
            if (_enableCache)
            	SetCacheItem('U', username, String.Join(",", ((string[])(results.ToArray(typeof(string))))));
            return ((string[])(results.ToArray(typeof(string))));
        }
        
        protected virtual void EnumerateGroups(PrincipalSearchResult<Principal> groups, ArrayList results)
        {
            foreach (GroupPrincipal group in groups)
            	if (!(_roleBlackList.Contains(group.SamAccountName)))
                	if (_isRoleWhiteListMode)
                    {
                        if (_roleWhiteList.Contains(group.SamAccountName))
                        {
                            results.Add(group.SamAccountName);
                            EnumerateGroups(group.GetGroups(), results);
                        }
                    }
                    else
                    {
                        results.Add(group.SamAccountName);
                        EnumerateGroups(group.GetGroups(), results);
                    }
        }
        
        public override string[] GetUsersInRole(string rolename)
        {
            if (!(RoleExists(rolename)))
            	throw new ProviderException(string.Format("The role \'0\' was not found.", rolename));
            if (_enableCache)
            {
                string CachedValue;
                CachedValue = GetCacheItem('R', rolename);
                if (CachedValue != "*NotCached")
                	return CachedValue.Split(',');
            }
            ArrayList results = new ArrayList();
            using (PrincipalContext context = new PrincipalContext(ADContextType, _server, _username, _password))
            	try
                {
                    GroupPrincipal p = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, rolename);
                    PrincipalSearchResult<Principal> users = p.GetMembers();
                    foreach (UserPrincipal user in users)
                    	if (!(_userBlackList.Contains(user.SamAccountName)))
                        	results.Add(user.SamAccountName);
                }
                catch (Exception ex)
                {
                    throw new ProviderException("Unable to query Active Directory.", ex);
                }
            if (_enableCache)
            	SetCacheItem('R', rolename, String.Join(",", ((string[])(results.ToArray(typeof(string))))));
            return ((string[])(results.ToArray(typeof(string))));
        }
        
        public override bool IsUserInRole(string username, string rolename)
        {
            foreach (string strUser in GetUsersInRole(username))
            	if (username == strUser)
                	return true;
            return false;
        }
        
        public override string[] GetAllRoles()
        {
            if (_enableCache)
            {
                string CachedValue;
                CachedValue = GetCacheItem('L', "AllRoles");
                if (CachedValue != "*NotCached")
                	return CachedValue.Split(',');
            }
            ArrayList results = new ArrayList();
            string[] roles = ADSearch(_activeDirectoryConnectionString, "(&(objectCategory=group)(|(groupType=-2147483646)(groupType=-2147483644)(groupTyp" +
                    "e=-2147483640)))", "samAccountName");
            foreach (string strRole in roles)
            	if (!(_roleBlackList.Contains(strRole)))
                	if (_isRoleWhiteListMode)
                    {
                        if (_roleWhiteList.Contains(strRole))
                        	results.Add(strRole);
                    }
                    else
                    	results.Add(strRole);
            if (_enableCache)
            	SetCacheItem('R', "AllRoles", String.Join(",", ((string[])(results.ToArray(typeof(string))))));
            return ((string[])(results.ToArray(typeof(string))));
        }
        
        public override bool RoleExists(string rolename)
        {
            foreach (string strRole in GetAllRoles())
            	if (rolename == strRole)
                	return true;
            return false;
        }
        
        public override string[] FindUsersInRole(string rolename, string usernameToMatch)
        {
            if (!(RoleExists(rolename)))
            	throw new ProviderException(string.Format("The role \'0\' was not found.", rolename));
            ArrayList results = new ArrayList();
            string[] roles = GetAllRoles();
            foreach (string role in roles)
            	if (role.ToLower().Contains(usernameToMatch.ToLower()))
                	results.Add(role);
            results.Sort();
            return ((string[])(results.ToArray(typeof(string))));
        }
        
        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            throw new NotSupportedException("Unable to add users to roles.  For security and management purposes, ADRoleProvid" +
                    "er only supports read operations against Active Directory.");
        }
        
        public override void CreateRole(string rolename)
        {
            throw new NotSupportedException("Unable to create new role.  For security and management purposes, ADRoleProvider " +
                    "only supports read operations against Active Directory.");
        }
        
        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
        {
            throw new NotSupportedException("Unable to delete role. For security and management purposes, ADRoleProvider only " +
                    "supports read operations against Active Directory.");
        }
        
        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
        {
            throw new NotSupportedException("Unable to remove users from roles. For security and management purposes, ADRolePr" +
                    "ovider only supports read operations against Active Directory.");
        }
        
        private string[] ADSearch(string connectionString, string filter, string field)
        {
            string strResults = "";
            DirectorySearcher searcher = new DirectorySearcher();
            searcher.SearchRoot = new DirectoryEntry(connectionString);
            searcher.Filter = filter;
            searcher.PropertiesToLoad.Clear();
            searcher.PropertiesToLoad.Add(field);
            searcher.PageSize = 500;
            SearchResultCollection results;
            try
            {
                results = searcher.FindAll();
            }
            catch (Exception ex)
            {
                throw new ProviderException("Unable to query Active Directory.", ex);
            }
            foreach (SearchResult result in results)
            {
                int resultCount = result.Properties[field].Count;
                for (int c = 0; (c < resultCount); c++)
                {
                    string temp = result.Properties[field][c].ToString();
                    strResults = (strResults 
                                + (temp + "|"));
                }
            }
            results.Dispose();
            if (strResults.Length > 0)
            {
                strResults = strResults.Substring(0, (strResults.Length - 1));
                return strResults.Split('|');
            }
            return new string[0];
        }
        
        private void SetCacheItem(char itemType, string itemKey, string itemValue)
        {
            HttpContext.Current.Cache.Add((itemType 
                            + ("-" + itemKey)), itemValue, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(Convert.ToDouble(_cacheTimeoutInMinutes)), CacheItemPriority.Default, null);
        }
        
        private string GetCacheItem(char itemType, string itemKey)
        {
            string value = ((string)(HttpContext.Current.Cache.Get((itemType 
                            + ("-" + itemKey)))));
            if (String.IsNullOrEmpty(value))
            	value = "*NotCached";
            return value;
        }
    }
}

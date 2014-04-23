using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Management.Client.Model
{
    public class UserInfo
    {
        private readonly string name;
        public string Password { get; private set; }
        public string Tags
        {
            get
            {
                return tagList.Any()
                    ? string.Join(",", tagList)
                    : allowedTags.First();

            }
        }
        private readonly ISet<string> allowedTags = new HashSet<string>
        {
            "administrator", "monitoring", "management", "policymaker"
        };

        private readonly ISet<string> tagList = new HashSet<string>();

        public UserInfo(string name, string password)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name is null or empty");
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("password is null or empty");
            }

            this.name = name;
            this.Password = password;
        }

        /// <summary>
        /// administrator: User can do everything monitoring can do, manage users, vhosts and permissions, close other user's connections, and manage policies and parameters for all vhosts.
        /// monitoring: User can access the management plugin and see all connections and channels as well as node-related information.
        /// management: User can access the management plugin
        /// policymaker: User can access the management plugin and manage policies and parameters for the vhosts they have access to.
        /// </summary>
        /// <param name="tag">One of the following tags: administrator, monitoring, management, policymaker</param>
        public UserInfo AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentException("tag is null or empty");
            }
            if (tagList.Contains(tag))
            {
                throw new ArgumentException(string.Format("tag '{0}' has already been added", tag));
            }
            if (!allowedTags.Contains(tag))
            {
                throw new ArgumentException(string.Format("tag '{0}' not recognised. Allowed tags are: {1}",
                    tag, string.Join(", ", allowedTags)));
            }

            tagList.Add(tag);
            return this;
        }

        public string GetName()
        {
            return name;
        }
    }
}
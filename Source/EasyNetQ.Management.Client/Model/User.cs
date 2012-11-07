namespace EasyNetQ.Management.Client.Model
{
    public class User
    {
        public string name { get; set; }
        public string password_hash { get; set; }
        public string tags { get; set; }
    }
}
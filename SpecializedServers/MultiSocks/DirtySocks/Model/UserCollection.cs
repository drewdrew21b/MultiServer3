using MultiSocks.DirtySocks.Messages;

namespace MultiSocks.DirtySocks.Model
{
    public class UserCollection
    {
        protected Dictionary<int, User> Users = new();
        protected Queue<int> UserIdsQueue = new(); // To maintain insertion order

        public List<User> GetAll()
        {
            lock (Users)
            {
                return UserIdsQueue.Select(id => Users[id]).ToList();
            }
        }

        public virtual bool AddUser(User? user, string VERS = "")
        {
            if (user == null)
                return false;

            lock (Users)
            {
                Users.Add(user.ID, user);
                UserIdsQueue.Enqueue(user.ID);
            }

            return true;
        }

        public virtual bool AddUserWithRoomMesg(User? user, string VERS = "")
        {
            if (user == null)
                return false;

            lock (Users)
            {
                Users.Add(user.ID, user);
                UserIdsQueue.Enqueue(user.ID);
            }

            return true;
        }

        public virtual void RemoveUser(User? user)
        {
            if (user == null)
                return;

            if (Users.ContainsKey(user.ID))
            {
                Users.Remove(user.ID);
                UserIdsQueue = new Queue<int>(UserIdsQueue.Where(id => id != user.ID));
            }
        }

        public virtual void UpdateUser(User user, User updatedUser)
        {
            if (Users.ContainsKey(user.ID))
                Users[user.ID] = updatedUser;
        }

        public User? GetUserByName(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return Users.FirstOrDefault(x => x.Value.Username == name).Value;
        }

        public User? GetUserByPersonaName(string name)
        {
            return Users.FirstOrDefault(x => x.Value.PersonaName == name).Value;
        }

        public int Count()
        {
            return Users.Count;
        }

        public void Broadcast(AbstractMessage msg)
        {
            byte[] data = msg.GetData();
            lock (Users)
            {
                foreach (var user in Users)
                {
                    user.Value.Connection?.SendMessage(data);
                }
            }
        }
    }
}

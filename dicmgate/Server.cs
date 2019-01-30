namespace dicmgate
{
    using Optimums.DICOM07;
    using System;
    using System.Collections;

    internal class Server : Network
    {
        public string AETitle = "";
        public static ArrayList AssociateList = new ArrayList();
        public static string[] BaseDirs = new string[] { "" };
        private bool canGet = true;
        public static string CharacterSet = "";
        private ArrayList clients = new ArrayList();
        public static string ConnectionString = "";
        public static int LoopCount = 0x2710;
        public uint MaxLength = 0;
        public static string OrderQuery = "";
        public string SaveDirectory = Environment.GetEnvironmentVariable("TEMP");
        public static string SiteId = "82.31.715.4551";

        public Client GetClient(int index)
        {
            if (this.canGet)
            {
                return (Client) this.clients[index];
            }
            return null;
        }

        public override void OnAccept()
        {
            base.OnAccept();
            Client client = new Client {
                CalledEntityTitle = this.AETitle,
                MaximumLength = this.MaxLength,
                Destination = this.SaveDirectory
            };
            base.Accept(client);
            this.clients.Add(client);
        }

        public override void OnClose(Network client)
        {
            base.OnClose(client);
            if (this.canGet)
            {
                this.canGet = false;
                try
                {
                    for (int i = 0; i < this.clients.Count; i++)
                    {
                        Client client2 = (Client) this.clients[i];
                        if (!client2.Connected)
                        {
                            this.clients.Remove(client2);
                            i--;
                        }
                    }
                }
                catch
                {
                }
                this.canGet = true;
            }
        }

        public void RemoveClient(Client client)
        {
            this.clients.Remove(client);
        }

        public int ClientCount
        {
            get
            {
                return this.clients.Count;
            }
        }
    }
}


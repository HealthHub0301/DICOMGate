namespace dicmgate
{
   using Optimums.DICOM07;
   using System;
   using System.Collections;
   using System.Data.Odbc;
   using System.Diagnostics;
   using System.IO;
   using System.Text;
   using System.Threading;
   using System.Text.RegularExpressions;

   internal class Client : Network
   {
      private Associate assoicate = new Associate(1);
      private DataSet commandSet = null;
      public DateTime CreationTime = DateTime.Now;
      private ArrayList dataList = new ArrayList();
      private Stream dataStream = null;
      public string Destination = Environment.GetEnvironmentVariable("TEMP");
      public DateTime LastReceiveTime = DateTime.Now;
      private ushort messageID = 0;
      public string Originator = "";
      private ushort responseST = 0;
      private Thread thread = null;
      public string storePath = "";
		public string Filename = DateTime.Now.ToString("yyyyMMddHHmm");
		//public string Filename = DateTime.Now.ToString("yyyyMMddHHmmssfff");


		public void Abort()
      {
         base.LogMessage("Disconnect: " + this.CallingEntityTitle);
         if (this.dataStream != null)
         {
            this.dataStream.Close();
            this.dataStream = null;
         }
         base.Close();
      }

      public int AddData(DataSet ds)
      {
         return this.dataList.Add(ds);
      }

      public virtual void DoCFind(object data)
      {
         DataSet ds = (DataSet)data;
         byte pid = 1;
         try
         {
            OdbcCommand command;
            OdbcDataReader reader;
            string orderQuery;
            DataElement element;
            OdbcConnection connection = new OdbcConnection(Server.ConnectionString);
            connection.Open();
            string str2 = "";
            string str3 = "";
            string str4 = "";
            string str5 = "";
            string str6 = "";
            string siteId = "";
            string str8 = "";
            string abstractSyntax = "";
            string str10 = "";
            foreach (PresentationContext context in this.assoicate.PresentationContexts)
            {
               if (context.AbstractSyntax == ds.SOPClassUID)
               {
                  abstractSyntax = context.AbstractSyntax;
                  str10 = (string)context.TransferSyntaxes[0];
                  pid = context.ID;
                  break;
               }
            }
            int order = 1;
            switch (str10)
            {
               case "1.2.840.10008.1.2":
                  order = 0;
                  break;

               case "1.2.840.10008.1.2.2":
                  order = 2;
                  break;
            }
            int length = 0;
            while (length < ds.Count)
            {
               element = ds[length];
               if (element.Tag == 0x100010)
               {
                  str2 = element.ToString();
               }
               else if (element.Tag == 0x100020)
               {
                  str3 = element.ToString();
               }
               else if (element.Tag == 0x80020)
               {
                  str5 = str4 = element.ToString();
               }
               else if (element.Tag == 0x80060)
               {
                  str6 = element.ToString();
               }
               else if ((str6 == "") && (element.Tag == 0x80061))
               {
                  str6 = element.ToString().Split(new char[] { '\\' })[0];
               }
               else if (element.Tag == 0x20000d)
               {
                  siteId = element.ToString();
               }
               else if (element.Tag == 0x20000e)
               {
                  str8 = element.ToString();
               }
               else if (element.Tag == 0x400002)
               {
                  str4 = element.ToString();
               }
               else if (element.Tag == 0x400004)
               {
                  str5 = element.ToString();
               }
               else if (((((element.Tag == 0x321064) || (element.Tag == 0x400100)) || ((element.Tag == 0x400008) || (element.Tag == 0xfffee000))) && (element.Length > 0)) && (element.Length < uint.MaxValue))
               {
                  MemoryStream input = new MemoryStream(element.GetValue(0, (int)element.Length, order));
                  DataSet set2 = DataSet.FromStream(input, order);
                  int num4 = 1;
                  element.SetValue((byte[])null);
                  element.Length = uint.MaxValue;
                  foreach (DataElement element2 in set2)
                  {
                     ds.Insert(length + num4++, element2);
                  }
                  if (element.Tag == 0xfffee000)
                  {
                     ds.Insert(length + num4, new DataElement(0xfffee00d, "", 0));
                  }
                  else
                  {
                     ds.Insert(length + num4, new DataElement(0xfffee0dd, "", 0));
                  }
               }
               length++;
            }
            if ((str4.Length > 8) && (str4.IndexOf('-') >= 0))
            {
               string[] strArray = str4.Split(new char[] { '-' });
               if (strArray[0] != "")
               {
                  str4 = strArray[0];
               }
               else
               {
                  str4 = "00000000";
               }
               if (strArray[1] != "")
               {
                  str5 = strArray[1];
               }
               else
               {
                  str5 = "99999999";
               }
            }
            else
            {
               if (str4 == "")
               {
                  str4 = "00000000";
               }
               if (str5 == "")
               {
                  if (str4 != "00000000")
                  {
                     str5 = str4;
                  }
                  else
                  {
                     str5 = "99999999";
                  }
               }
            }
            if (abstractSyntax == "1.2.840.10008.5.1.4.31")
            {
               if (Server.OrderQuery == "")
               {
                  command = new OdbcCommand("select SqlString from SQLCOMMAND where Name = 'OrderQuery'", connection);
                  reader = command.ExecuteReader();
                  if (!reader.Read())
                  {
                     reader.Close();
                     this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xfe00, null);
                     base.LogMessage("CFind Response: 0xFE00.");
                     connection.Close();
                     return;
                  }
                  orderQuery = reader.GetString(0);
                  reader.Close();
               }
               else
               {
                  orderQuery = Server.OrderQuery;
               }
               if ((ds[ds.Count - 1].Tag < 0x20000d) && (ds[0x20000d] == null))
               {
                  ds.Add(new DataElement(0x20000d, "UI", 0));
               }
               if ((ds[ds.Count - 1].Tag < 0x201206) && (ds[0x201206] == null))
               {
                  ds.Add(new DataElement(0x201206, "IS", 0));
               }
               if ((ds[ds.Count - 1].Tag < 0x201208) && (ds[0x201208] == null))
               {
                  ds.Add(new DataElement(0x201208, "IS", 0));
               }
               string str11 = "";
               command = new OdbcCommand(string.Format(orderQuery, new object[] { str2.TrimEnd(new char[] { '*' }), str3.TrimEnd(new char[] { '*' }), str4, str5, str6, "", "" }), connection);
               base.LogMessage(command.CommandText);
               reader = command.ExecuteReader();
               while (reader.Read())
               {
                  string str12;
                  if (reader.IsDBNull(7))
                  {
                     str12 = "";
                  }
                  else if (!(reader.IsDBNull(10) || (reader.GetInt32(10) <= 0)))
                  {
                     str12 = string.Format("{0}{1:00000}", reader.GetString(7).Trim(), reader.GetInt32(10));
                  }
                  else
                  {
                     str12 = reader.GetString(7).Trim();
                  }
                  for (int i = 0; i < ds.Count; i++)
                  {
                     element = ds[i];
                     if (ds[i].ElementNumber == 0)
                     {
                        ds.RemoveAt(i--);
                     }
                     else if ((str11 == "") && (element.Tag == 0x80005))
                     {
                        str11 = element.ToString().Trim();
                        if (str11 == "")
                        {
                           str11 = "Unknown";
                        }
                        base.LogMessage("Character Set: " + str11);
                     }
                     else
                     {
                        string str14;
                        byte[] bytes;
                        byte[] buffer2;
                        if ((element.Tag == 0x100010) && !reader.IsDBNull(0))
                        {
                           string[] strArray2 = reader.GetString(0).Split(new char[] { ';' });
                           if (strArray2.Length > 1)
                           {
                              element.SetValue(strArray2[1].Trim());
                           }
                           else
                           {
                              str14 = strArray2[0].Trim();
                              if (str11 == @"\ISO 2022 IR 149")
                              {
                                 bytes = Encoding.Default.GetBytes(str14);
                                 if ((bytes != null) && (bytes.Length > 0))
                                 {
                                    buffer2 = new byte[4 + bytes.Length];
                                    buffer2[0] = 0x1b;
                                    buffer2[1] = 0x24;
                                    buffer2[2] = 0x29;
                                    buffer2[3] = 0x43;
                                    Array.Copy(bytes, 0, buffer2, 4, bytes.Length);
                                    element.SetValue(buffer2);
                                 }
                              }
                              else if (File.Exists("kor2eng.exe"))
                              {
                                 if (File.Exists("name.eng"))
                                 {
                                    File.Delete("name.eng");
                                 }
                                 Process process = Process.Start("kor2eng.exe", string.Concat(new object[] { '"', str14, '"', " name.eng" }));
                                 for (int j = 0; !process.HasExited && (j < 300); j++)
                                 {
                                    Thread.Sleep(10);
                                 }
                                 if (File.Exists("name.eng"))
                                 {
                                    StreamReader reader2 = new StreamReader("name.eng");
                                    string str15 = reader2.ReadLine();
                                    reader2.Close();
                                    if (string.IsNullOrEmpty(str15))
                                    {
                                       element.SetValue(str14);
                                    }
                                    else
                                    {
                                       element.SetValue(str15.Trim());
                                    }
                                 }
                                 else
                                 {
                                    element.SetValue(str14);
                                 }
                              }
                              else
                              {
                                 element.SetValue(str14);
                              }
                           }
                        }
                        else if (!((element.Tag != 0x100020) || reader.IsDBNull(1)))
                        {
                           element.SetValue(reader.GetString(1).Trim());
                        }
                        else if (!((element.Tag != 0x100030) || reader.IsDBNull(2)))
                        {
                           element.SetValue(reader.GetString(2).Trim());
                        }
                        else if (!((element.Tag != 0x100040) || reader.IsDBNull(3)))
                        {
                           element.SetValue(reader.GetString(3).Trim());
                        }
                        else if (element.Tag == 0x1021c0)
                        {
                           element.SetValue("");
                        }
                        else if (!((element.Tag != 0x80090) || reader.IsDBNull(4)))
                        {
                           element.SetValue(reader.GetString(4).Trim());
                        }
                        else
                        {
                           string str13;
                           if (element.Tag == 0x20000d)
                           {
                              str13 = str12;
                              siteId = Server.SiteId;
                              if (str13 != null)
                              {
                                 length = 0;
                                 while (length < str13.Length)
                                 {
                                    siteId = siteId + "." + ((int)str13[length]);
                                    length++;
                                 }
                              }
                              element.SetValue(siteId);
                           }
                           else if (((element.Tag == 0x80020) || (element.Tag == 0x400002)) && !reader.IsDBNull(6))
                           {
                              str13 = reader.GetString(6);
                              if (str13.Length > 8)
                              {
                                 element.SetValue(str13.Substring(0, 8));
                              }
                              else
                              {
                                 element.SetValue(str13);
                              }
                           }
                           else if (((element.Tag == 0x80030) || (element.Tag == 0x400003)) && !reader.IsDBNull(6))
                           {
                              str13 = reader.GetString(6).Trim();
                              if (str13.Length > 8)
                              {
                                 length = str13.Length;
                                 while (length < 14)
                                 {
                                    str13 = str13 + "0";
                                    length++;
                                 }
                                 element.SetValue(str13.Substring(8, 6));
                              }
                           }
                           else if (element.Tag == 0x400001)
                           {
                              element.SetValue(this.CallingEntityTitle);
                           }
                           else if (element.Tag == 0x400004)
                           {
                              element.SetValue("");
                           }
                           else if (!((((element.Tag != 0x400009) && (element.Tag != 0x401001)) && (element.Tag != 0x80100)) || reader.IsDBNull(5)))
                           {
                              element.SetValue(reader.GetString(5).Trim());
                           }
                           else if (element.Tag == 0x80050)
                           {
                              element.SetValue(str12);
                           }
                           else if (!((element.Tag != 0x80060) || reader.IsDBNull(8)))
                           {
                              element.SetValue(reader.GetString(8).Trim());
                           }
                           else if (!(((element.Tag != 0x80102) || (reader.FieldCount <= 13)) || reader.IsDBNull(13)))
                           {
                              element.SetValue(reader.GetString(13).Trim());
                           }
                           else if ((((element.Tag == 0x81030) || (element.Tag == 0x321060)) || ((element.Tag == 0x400007) || (element.Tag == 0x80104))) && !reader.IsDBNull(9))
                           {
                              str14 = reader.GetString(9).Trim();
                              if (str11 == @"\ISO 2022 IR 149")
                              {
                                 bytes = Encoding.Default.GetBytes(str14);
                                 if ((bytes != null) && (bytes.Length > 0))
                                 {
                                    buffer2 = new byte[4 + bytes.Length];
                                    buffer2[0] = 0x1b;
                                    buffer2[1] = 0x24;
                                    buffer2[2] = 0x29;
                                    buffer2[3] = 0x43;
                                    Array.Copy(bytes, 0, buffer2, 4, bytes.Length);
                                    element.SetValue(buffer2);
                                 }
                              }
                              else
                              {
                                 element.SetValue(str14);
                              }
                           }
                           else if (!((element.Tag != 0x201206) || reader.IsDBNull(10)))
                           {
                              element.SetValue(reader.GetInt16(10).ToString());
                           }
                           else if (!((element.Tag != 0x201208) || reader.IsDBNull(11)))
                           {
                              element.SetValue(reader.GetInt16(11).ToString());
                           }
                           else if (!((element.Tag != 0x200010) || reader.IsDBNull(12)))
                           {
                              element.SetValue(reader.GetString(12).Trim());
                           }
                        }
                     }
                  }
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xff00, ds);
                  base.LogMessage("CFind Response: 0xFF00");
               }
               reader.Close();
            }
            else if (siteId != "")
            {
               command = new OdbcCommand("select SqlString from SQLCOMMAND where Name = 'SeriesList'", connection);
               reader = command.ExecuteReader();
               if (!reader.Read())
               {
                  reader.Close();
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xfe00, null);
                  base.LogMessage("CFind Response: 0xFE00.");
                  connection.Close();
                  return;
               }
               orderQuery = reader.GetString(0);
               reader.Close();
               ds.Clear();
               ds.Add(new DataElement(0x80052, "CS", 0));
               ds.Add(new DataElement(0x80060, "CS", 0));
               ds.Add(new DataElement(0x8103e, "LO", 0));
               ds.Add(new DataElement(0x20000e, "UI", 0));
               ds.Add(new DataElement(0x200011, "IS", 0));
               ds.Add(new DataElement(0x201209, "IS", 0));
               command.CommandText = string.Format(orderQuery, siteId);
               reader = command.ExecuteReader();
               while (reader.Read())
               {
                  foreach (DataElement element1 in ds)
                  {
                     if (element1.Tag == 0x80052)
                     {
                        element1.SetValue("SERIES");
                     }
                     else if (element1.Tag == 0x80060)
                     {
                        element1.SetValue(reader.GetString(0));
                     }
                     else if (element1.Tag == 0x20000e)
                     {
                        element1.SetValue(reader.GetString(1));
                     }
                     else if (element1.Tag == 0x8103e)
                     {
                        element1.SetValue(reader.GetString(2));
                     }
                     else if (element1.Tag == 0x200011)
                     {
                        element1.SetValue(reader.GetInt32(3).ToString());
                     }
                     else if (element1.Tag == 0x201209)
                     {
                        element1.SetValue(reader.GetInt16(4).ToString());
                     }
                  }
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xff00, ds);
                  base.LogMessage("CFind Response: 0xFF00");
               }
               reader.Close();
            }
            else if (str8 != "")
            {
               command = new OdbcCommand("select SqlString from SQLCOMMAND where Name = 'ImageList'", connection);
               reader = command.ExecuteReader();
               if (!reader.Read())
               {
                  reader.Close();
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xfe00, null);
                  base.LogMessage("CFind Response: 0xFE00.");
                  connection.Close();
                  return;
               }
               orderQuery = reader.GetString(0);
               reader.Close();
               ds.Clear();
               ds.Add(new DataElement(0x80018, "UI", 0));
               ds.Add(new DataElement(0x80052, "CS", 0));
               ds.Add(new DataElement(0x200013, "IS", 0));
               command.CommandText = string.Format(orderQuery, str8);
               reader = command.ExecuteReader();
               while (reader.Read())
               {
                  foreach (DataElement element1 in ds)
                  {
                     if (element1.Tag == 0x80052)
                     {
                        element1.SetValue("IMAGE");
                     }
                     else if (element1.Tag == 0x80018)
                     {
                        element1.SetValue(reader.GetString(0));
                     }
                     else if (element1.Tag == 0x200013)
                     {
                        element1.SetValue(reader.GetInt32(1).ToString());
                     }
                  }
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xff00, ds);
                  base.LogMessage("CFind Response: 0xFF00");
               }
               reader.Close();
            }
            else
            {
               command = new OdbcCommand("select SqlString from SQLCOMMAND where Name = 'WorklistQuery'", connection);
               reader = command.ExecuteReader();
               if (!reader.Read())
               {
                  reader.Close();
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xfe00, null);
                  base.LogMessage("CFind Response: 0xFE00.");
                  connection.Close();
                  return;
               }
               orderQuery = reader.GetString(0);
               reader.Close();
               if ((ds[ds.Count - 1].Tag < 0x20000d) && (ds[0x20000d] == null))
               {
                  ds.Add(new DataElement(0x20000d, "UI", 0));
               }
               if ((ds[ds.Count - 1].Tag < 0x201206) && (ds[0x201206] == null))
               {
                  ds.Add(new DataElement(0x201206, "IS", 0));
               }
               if ((ds[ds.Count - 1].Tag < 0x201208) && (ds[0x201208] == null))
               {
                  ds.Add(new DataElement(0x201208, "IS", 0));
               }
               command.CommandText = string.Format(orderQuery, new object[] { str2.TrimEnd(new char[] { '*' }), str3.TrimEnd(new char[] { '*' }), str4, str5, str6, "", "" });
               reader = command.ExecuteReader();
               while (reader.Read())
               {
                  foreach (DataElement element1 in ds)
                  {
                     if (!(reader.IsDBNull(0) || (element1.Tag != 0x100010)))
                     {
                        element1.SetValue(reader.GetString(0));
                     }
                     else if (!(reader.IsDBNull(1) || (element1.Tag != 0x100020)))
                     {
                        element1.SetValue(reader.GetString(1));
                     }
                     else if (!(reader.IsDBNull(2) || (element1.Tag != 0x100030)))
                     {
                        element1.SetValue(reader.GetString(2));
                     }
                     else if (!(reader.IsDBNull(3) || (element1.Tag != 0x100040)))
                     {
                        element1.SetValue(reader.GetString(3));
                     }
                     else if (!(reader.IsDBNull(5) || (element1.Tag != 0x20000d)))
                     {
                        element1.SetValue(reader.GetString(5));
                     }
                     else if (!(reader.IsDBNull(6) || (element1.Tag != 0x80020)))
                     {
                        element1.SetValue(reader.GetString(6));
                     }
                     else if (!(reader.IsDBNull(6) || (element1.Tag != 0x400002)))
                     {
                        element1.SetValue(reader.GetString(6));
                     }
                     else if (element1.Tag == 0x400004)
                     {
                        element1.SetValue("");
                     }
                     else if (!(reader.IsDBNull(7) || (element1.Tag != 0x80050)))
                     {
                        element1.SetValue(reader.GetString(7));
                     }
                     else if (!(reader.IsDBNull(8) || (element1.Tag != 0x80060)))
                     {
                        element1.SetValue(reader.GetString(8));
                     }
                     else if (!(reader.IsDBNull(9) || (element1.Tag != 0x81030)))
                     {
                        element1.SetValue(reader.GetString(9));
                     }
                     else if (!(reader.IsDBNull(10) || (element1.Tag != 0x201206)))
                     {
                        element1.SetValue(reader.GetValue(10).ToString());
                     }
                     else if (!(reader.IsDBNull(11) || (element1.Tag != 0x201208)))
                     {
                        element1.SetValue(reader.GetValue(11).ToString());
                     }
                     else if (!(reader.IsDBNull(12) || (element1.Tag != 0x200010)))
                     {
                        element1.SetValue(reader.GetString(12));
                     }
                  }
                  this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xff00, ds);
                  base.LogMessage("CFind Response: 0xFF00");
               }
               reader.Close();
            }
            this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0, null);
            base.LogMessage("CFind Response: 0x0000");
            connection.Close();
            for (length = 0; base.Connected && (length < 100); length++)
            {
               Thread.Sleep(100);
            }
         }
         catch (Exception exception)
         {
            this.SendCFindRSP(pid, ds.SOPClassUID, this.messageID, 0xfe00, null);
            base.LogMessage(exception.ToString());
            base.Close();
         }
      }

      public virtual void DoCGet(object data)
      {
         DataSet set = (DataSet)data;
         byte pid = 1;
         OdbcConnection connection = null;
         try
         {
            connection = new OdbcConnection(Server.ConnectionString);
            connection.Open();
            string str4 = "";
            string str5 = "";
            string str6 = "";
            foreach (PresentationContext context in this.assoicate.PresentationContexts)
            {
               if ((context.AbstractSyntax == set.SOPClassUID) && (((string)context.TransferSyntaxes[0]) == set.TransferSyntaxUID))
               {
                  pid = context.ID;
                  break;
               }
            }
            foreach (DataElement element in set)
            {
               if (element.Tag == 0x20000d)
               {
                  str4 = element.ToString();
               }
               else if (element.Tag == 0x20000e)
               {
                  str5 = element.ToString();
               }
               else if (element.Tag == 0x80018)
               {
                  str6 = element.ToString();
               }
            }
            OdbcCommand command = new OdbcCommand("select SqlString from SQLCOMMAND where Name = 'ImageQuery'", connection);
            OdbcDataReader reader = command.ExecuteReader();
            if (!reader.Read())
            {
               reader.Close();
               this.SendCGetRSP(pid, set.SOPClassUID, this.messageID, 0xfe00, 0, 0, 0, 0, null);
               base.LogMessage("CGet Response: 0xFE00 - No Image.");
               connection.Close();
            }
            else
            {
               DataSet set2;
               string format = reader.GetString(0);
               reader.Close();
               command.CommandText = string.Format(format, str4, str5, str6);
               reader = command.ExecuteReader();
               while (reader.Read())
               {
                  foreach (string str8 in Server.BaseDirs)
                  {
                     string path = str8 + @"\" + reader.GetString(0);
                     if (File.Exists(path) && ((set2 = DataSet.FromFile(path)) != null))
                     {
                        base.LogMessage(path);
                        this.dataList.Add(set2);
                        break;
                     }
                  }
               }
               reader.Close();
               if (this.DataCount > 0)
               {
                  byte iD = 0;
                  ushort messageID = this.messageID;
                  this.messageID = (ushort)this.DataCount;
                  set2 = this.GetData(0);
                  string sOPClassUID = set2.SOPClassUID;
                  string transferSyntaxUID = set2.TransferSyntaxUID;
                  foreach (PresentationContext context in this.assoicate.PresentationContexts)
                  {
                     if ((context.AbstractSyntax == sOPClassUID) && (((((string)context.TransferSyntaxes[0]) == transferSyntaxUID) || (((string)context.TransferSyntaxes[0]) == "1.2.840.10008.1.2")) || (((string)context.TransferSyntaxes[0]) == "1.2.840.10008.1.2.1")))
                     {
                        iD = context.ID;
                        break;
                     }
                  }
                  if (iD > 0)
                  {
                     this.SendCGetRSP(pid, set.SOPClassUID, messageID, 0xff00, (ushort)this.DataCount, 0, 0, 0, null);
                     base.LogMessage("CGet Response: 0xFF00 - 0");
                     this.SendCStoreRQ(iD, sOPClassUID, 1, 0, set2.SOPInstanceUID, "", 0, set2);
                     this.messageID = 0;
                     for (int i = 0; (base.Connected && (this.messageID < this.DataCount)) && (i < Server.LoopCount); i++)
                     {
                        if (this.messageID > 0)
                        {
                           this.SendCGetRSP(pid, set.SOPClassUID, messageID, 0xff00, (ushort)(this.DataCount - this.messageID), this.messageID, 0, 0, null);
                           base.LogMessage("CGet Response: 0xFF00 - " + this.messageID);
                           set2 = this.GetData(this.messageID);
                           if (set2.SOPClassUID != sOPClassUID)
                           {
                              sOPClassUID = set2.SOPClassUID;
                              transferSyntaxUID = set2.TransferSyntaxUID;
                              foreach (PresentationContext context in this.assoicate.PresentationContexts)
                              {
                                 if ((context.AbstractSyntax == sOPClassUID) && (((((string)context.TransferSyntaxes[0]) == transferSyntaxUID) || (((string)context.TransferSyntaxes[0]) == "1.2.840.10008.1.2")) || (((string)context.TransferSyntaxes[0]) == "1.2.840.10008.1.2.1")))
                                 {
                                    iD = context.ID;
                                    break;
                                 }
                              }
                           }
                           ushort mid = (ushort)(this.messageID + 1);
                           this.messageID = 0;
                           i = 0;
                           this.SendCStoreRQ(iD, sOPClassUID, mid, 0, set2.SOPInstanceUID, "", 0, set2);
                        }
                        Thread.Sleep(100);
                     }
                     this.SendCGetRSP(pid, set.SOPClassUID, messageID, 0xff00, 0, this.messageID, 0, 0, null);
                     base.LogMessage("CGet Response: 0xFF00 - " + this.messageID);
                     this.SendCGetRSP(pid, set.SOPClassUID, messageID, 0, 0, 0, 0, 0, null);
                     base.LogMessage("CGet Response: 0x0000.");
                  }
                  else
                  {
                     this.SendCGetRSP(pid, set.SOPClassUID, this.messageID, 0xfe00, 0, 0, 0, 0, null);
                     base.LogMessage("CGet Response: 0xFE00.");
                  }
                  foreach (DataSet set3 in this.dataList)
                  {
                     set3.Dispose();
                  }
                  this.dataList.Clear();
               }
               else
               {
                  this.SendCGetRSP(pid, set.SOPClassUID, this.messageID, 0, 0, 0, 0, 0, null);
                  base.LogMessage("CGet Response: No Image.");
               }
               connection.Close();
            }
         }
         catch (Exception exception)
         {
            this.SendCGetRSP(pid, set.SOPClassUID, this.messageID, 0xfe00, 0, 0, 0, 0, null);
            base.LogMessage(exception.ToString());
            if (connection != null)
            {
               connection.Close();
            }
            if (this.dataList.Count > 0)
            {
               foreach (DataSet set3 in this.dataList)
               {
                  set3.Dispose();
               }
               this.dataList.Clear();
            }
            base.Close();
         }
      }

      public virtual void DoCMove(object data)
      {
         DataSet set = (DataSet)data;
         byte pid = 1;
         try
         {
            OdbcConnection connection = new OdbcConnection(Server.ConnectionString);
            connection.Open();
            string format = "";
            string str2 = "";
            string str3 = "";
            string str4 = "";
            string str5 = "";
            foreach (PresentationContext context in this.assoicate.PresentationContexts)
            {
               if (context.AbstractSyntax == set.SOPClassUID)
               {
                  pid = context.ID;
                  break;
               }
            }
            foreach (DataElement element in set)
            {
               if (element.Tag == 0x20000d)
               {
                  str3 = element.ToString();
               }
               else if (element.Tag == 0x20000e)
               {
                  str4 = element.ToString();
               }
               else if (element.Tag == 0x80018)
               {
                  str5 = element.ToString();
               }
            }
            OdbcCommand command = new OdbcCommand("select Name, SqlString from SQLCOMMAND", connection);
            OdbcDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
               if (reader.GetString(0) == "ImageQuery")
               {
                  format = reader.GetString(1);
               }
               else if (reader.GetString(0) == "ClientQuery")
               {
                  str2 = reader.GetString(1);
               }
            }
            reader.Close();
            Client client = new Client();
            command.CommandText = string.Format(format, str3, str4, str5);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
               foreach (string str7 in Server.BaseDirs)
               {
                  DataSet set2;
                  string path = str7 + @"\" + reader.GetString(0);
                  if (File.Exists(path) && ((set2 = DataSet.FromFile(path)) != null))
                  {
                     base.LogMessage(path);
                     client.dataList.Add(set2);
                     break;
                  }
               }
            }
            reader.Close();
            if (client.DataCount <= 0)
            {
               goto Label_05E5;
            }
            command.CommandText = string.Format(str2, this.Destination);
            base.LogMessage(command.CommandText);
            reader = command.ExecuteReader();
            if (!reader.Read())
            {
               reader.Close();
               this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0xfe00, 0, 0, 0, 0, null);
               base.LogMessage("CMove Response: 0xFE00 - Retrieve.");
               connection.Close();
               foreach (DataSet set3 in client.dataList)
               {
                  set3.Dispose();
               }
               client.dataList.Clear();
               return;
            }
            client.CallingEntityTitle = reader.GetString(0);
            client.CalledEntityTitle = this.Destination;
            client.Originator = this.CallingEntityTitle;
            string host = reader.GetString(1);
            if (host == "0.0.0.0")
            {
               host = base.RemoteAddress;
            }
            client.Connect(host, reader.GetInt32(2));
            reader.Close();
            Thread.Sleep(0x3e8);
            ushort cc = 0;
            this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0xff00, (ushort)client.DataCount, 0, 0, 0, null);
            base.LogMessage("CMove Response: 0xFF00 - 0");
            for (int i = 0; (client.Connected && (client.messageID < client.DataCount)) && (i < 0x3e8); i++)
            {
               if (client.messageID <= cc)
               {
                  goto Label_04C2;
               }
               goto Label_049E;
               Label_045B:
               this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0xff00, (ushort)(client.DataCount - cc), cc, 0, 0, null);
               base.LogMessage("CMove Response: 0xFF00 - " + cc);
               Label_049E:
               cc = (ushort)(cc + 1);
               if (cc < client.messageID)
               {
                  goto Label_045B;
               }
               cc = client.messageID;
               i = 0;
               Label_04C2:
               Thread.Sleep(100);
            }
            goto Label_0544;
            Label_0501:
            this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0xff00, (ushort)(client.DataCount - cc), cc, 0, 0, null);
            base.LogMessage("CMove Response: 0xFF00 - " + cc);
            Label_0544:
            cc = (ushort)(cc + 1);
            if (cc < client.messageID)
            {
               goto Label_0501;
            }
            this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0, 0, 0, 0, 0, null);
            base.LogMessage("CMove Response: 0x0000.");
            foreach (DataSet set3 in client.dataList)
            {
               set3.Dispose();
            }
            client.dataList.Clear();
            goto Label_060D;
            Label_05E5:
            this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0, 0, 0, 0, 0, null);
            base.LogMessage("CMove Response: No Image.");
            Label_060D:
            connection.Close();
         }
         catch (Exception exception)
         {
            this.SendCMoveRSP(pid, set.SOPClassUID, this.messageID, 0xfe00, 0, 0, 0, 0, null);
            base.LogMessage(exception.ToString());
            base.Close();
         }
      }

      public DataSet GetData(int index)
      {
         return ((DataSet)this.dataList[index]);
      }

      public override void OnConnect()
      {
         base.OnConnect();
         base.LogMessage("Connected.");
         PresentationContext context = null;
         foreach (DataSet set in this.dataList)
         {
            if (context == null)
            {
               context = new PresentationContext(1, 0, set.SOPClassUID);
               context.TransferSyntaxes.Add(set.TransferSyntaxUID);
               if (set.TransferSyntaxUID != "1.2.840.10008.1.2")
               {
                  context.TransferSyntaxes.Add("1.2.840.10008.1.2");
               }
               this.assoicate.PresentationContexts.Add(context);
            }
            else if (context.AbstractSyntax != set.SOPClassUID)
            {
               context = new PresentationContext((byte)(context.ID + 2), 0, set.SOPClassUID);
               context.TransferSyntaxes.Add(set.TransferSyntaxUID);
               if (set.TransferSyntaxUID != "1.2.840.10008.1.2")
               {
                  context.TransferSyntaxes.Add("1.2.840.10008.1.2");
               }
               this.assoicate.PresentationContexts.Add(context);
            }
         }
         if (context == null)
         {
            context = new PresentationContext(1, 0, "1.2.840.10008.1.1");
            context.TransferSyntaxes.Add("1.2.840.10008.1.2");
            this.assoicate.PresentationContexts.Add(context);
         }
         this.assoicate.ImplementationClassUID = Server.SiteId;
         base.SendAssociateRQ(this.assoicate);
      }

      public override void OnError()
      {
         base.OnError();
         if (this.dataStream != null)
         {
            this.dataStream.Close();
            this.dataStream = null;
         }
         if (base.Connected)
         {
            base.Close();
         }
      }

      public override void OnReceiveAbort(byte source, byte reason)
      {
         base.OnReceiveAbort(source, reason);
         base.LogMessage(string.Concat(new object[] { "Aborted: ", source, ", ", reason }));
         base.Close();
      }

      public override void OnReceiveAssociateAC(Associate ac)
      {
         base.OnReceiveAssociateAC(ac);
         base.LogMessage("Associated: " + this.assoicate.CalledEntityTitle);
         foreach (PresentationContext context in this.assoicate.PresentationContexts)
         {
            foreach (PresentationContext context2 in ac.PresentationContexts)
            {
               if (context2.ID == context.ID)
               {
                  context.TransferSyntaxes.Clear();
                  if (context2.Result == 0)
                  {
                     context.TransferSyntaxes.Add(context2.TransferSyntaxes[0]);
                  }
                  else
                  {
                     context.TransferSyntaxes.Add("");
                  }
                  ac.PresentationContexts.Remove(context2);
                  break;
               }
            }
         }
         this.assoicate.MaximumLength = ac.MaximumLength;
         if ((this.dataList == null) || (this.dataList.Count == 0))
         {
            this.SendCEchoRQ(1, "1.2.840.10008.1.1", 1);
         }
         else
         {
            DataSet ds = (DataSet)this.dataList[0];
            if (ds.SOPClassUID.StartsWith("1.2.840.10008.5.1.4.1.1."))
            {
               this.SendCStoreRQ(1, ds.SOPClassUID, 1, 0, ds.SOPInstanceUID, this.Originator, 1, ds);
            }
            else if (((ds.SOPClassUID == "1.2.840.10008.5.1.4.31") || (ds.SOPClassUID == "1.2.840.10008.5.1.4.1.2.1.1")) || (ds.SOPClassUID == "1.2.840.10008.5.1.4.1.2.2.1"))
            {
               this.SendCFindRQ(1, ds.SOPClassUID, 1, 0, ds);
            }
            else if ((ds.SOPClassUID == "1.2.840.10008.5.1.4.1.2.1.3") || (ds.SOPClassUID == "1.2.840.10008.5.1.4.1.2.2.3"))
            {
               this.SendCGetRQ(1, ds.SOPClassUID, 1, 0, ds);
            }
            else if ((ds.SOPClassUID == "1.2.840.10008.5.1.4.1.2.1.2") || (ds.SOPClassUID == "1.2.840.10008.5.1.4.1.2.2.2"))
            {
               this.SendCMoveRQ(1, ds.SOPClassUID, 1, 0, this.Destination, ds);
            }
            else
            {
               base.SendReleaseRQ();
            }
         }
      }

      public override void OnReceiveAssociateRJ(byte result, byte source, byte reason)
      {
         base.OnReceiveAssociateRJ(result, source, reason);
         base.LogMessage(string.Concat(new object[] { "Rejected: ", result, ", ", source, ", ", reason }));
         base.Close();
      }

      public override void OnReceiveAssociateRQ(Associate rq)
      {
         base.OnReceiveAssociateRQ(rq);
         if ((this.assoicate.CalledEntityTitle != "") && (this.assoicate.CalledEntityTitle.ToUpper() != rq.CalledEntityTitle.ToUpper()))
         {
            base.SendAssociateRJ(1, 1, 7);
            base.LogMessage("Reject: Called AE Title is not recognized - " + rq.CalledEntityTitle);
         }
         else if ((this.assoicate.CallingEntityTitle != "") && (this.assoicate.CallingEntityTitle.ToUpper() != rq.CallingEntityTitle.ToUpper()))
         {
            base.SendAssociateRJ(1, 1, 3);
            base.LogMessage("Reject: Calling AE Title is not recognized - " + rq.CallingEntityTitle);
         }
         else
         {
            if (Server.AssociateList.Count > 0)
            {
               bool flag = false;
               foreach (string str in Server.AssociateList)
               {
                  if ((str == base.RemoteAddress) || (str == rq.CallingEntityTitle))
                  {
                     flag = true;
                     break;
                  }
               }
               if (!flag)
               {
                  base.LogMessage("Abort: Invalid IP Address - " + base.RemoteAddress);
                  base.SendAbort(0, 0);
                  base.Close();
                  return;
               }
            }
            base.LogMessage("Associate: " + rq.CallingEntityTitle);
            foreach (PresentationContext context in rq.PresentationContexts)
            {
               if (((!context.AbstractSyntax.StartsWith("1.2.840.10008.5.1.4.1.1.") && !context.AbstractSyntax.StartsWith("1.2.840.10008.5.1.4.1.2.")) && (context.AbstractSyntax != "1.2.840.10008.1.1")) && (context.AbstractSyntax != "1.2.840.10008.5.1.4.31"))
               {
                  context.Result = 3;
                  base.LogMessage("Reject: " + context.AbstractSyntax);
               }
               else
               {
                  context.Result = 0;
                  base.LogMessage("Abstract Syntax UID: " + context.AbstractSyntax);
                  for (int i = 1; i < context.TransferSyntaxes.Count; i++)
                  {
                     if (((((string)context.TransferSyntaxes[i]) == "1.2.840.10008.1.2") && (((string)context.TransferSyntaxes[0]) != "1.2.840.10008.1.2.1")) && (((string)context.TransferSyntaxes[0]) != "1.2.840.10008.1.2.4.90"))
                     {
                        context.TransferSyntaxes[0] = "1.2.840.10008.1.2";
                     }
                     else if (((((string)context.TransferSyntaxes[i]) == "1.2.840.10008.1.2.1") && (((string)context.TransferSyntaxes[0]) != "1.2.840.10008.1.2")) && (((string)context.TransferSyntaxes[0]) != "1.2.840.10008.1.2.4.90"))
                     {
                        context.TransferSyntaxes[0] = "1.2.840.10008.1.2.1";
                     }
                  }
                  if (context.TransferSyntaxes.Count > 0)
                  {
                     base.LogMessage("Transfer Syntax UID: " + context.TransferSyntaxes[0]);
                  }
               }
            }
            if (this.assoicate.MaximumLength > 0)
            {
               rq.MaximumLength = this.assoicate.MaximumLength;
            }
            this.assoicate = rq;
            if (this.Destination.IndexOf("[CALLING_TITLE]") > 0)
            {
               this.Destination = this.Destination.Replace("[CALLING_TITLE]", rq.CallingEntityTitle);
               if (!Directory.Exists(this.Destination))
               {
                  Directory.CreateDirectory(this.Destination);
               }
            }
            else if (this.Destination.IndexOf("[CALLED_TITLE]") > 0)
            {
               this.Destination = this.Destination.Replace("[CALLED_TITLE]", rq.CalledEntityTitle);
               if (!Directory.Exists(this.Destination))
               {
                  Directory.CreateDirectory(this.Destination);
               }
            }


            //this.storePath = this.Destination + @"\" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            //if (!Directory.Exists(this.storePath))
            //{
            //    Directory.CreateDirectory(this.storePath);
            //}


            base.SendAssociateAC(this.assoicate);
         }
      }

      public virtual void OnReceiveCCancelRQ(byte pid, ushort mid)
      {
         base.LogMessage("CCancel Request.");
         if ((this.thread != null) && this.thread.IsAlive)
         {
            this.messageID = 0;
         }
      }

      public virtual void OnReceiveCEchoRQ(byte pid, string sopClass, ushort mid)
      {
         base.LogMessage("CEcho Request.");
         this.SendCEchoRSP(pid, sopClass, mid, 0);
      }

      public virtual void OnReceiveCEchoRSP(byte pid, string sopClass, ushort mid, ushort status)
      {
         base.LogMessage("CEcho Response: " + status);
         base.SendReleaseRQ();
      }

      public virtual void OnReceiveCFindRQ(byte pid, string sopClass, ushort mid, ushort prioirty, DataSet ds)
      {
         base.LogMessage("CFind Request: " + sopClass);
         this.messageID = mid;
         this.thread = new Thread(new ParameterizedThreadStart(this.DoCFind));
         this.thread.Start(ds);
      }

      public virtual void OnReceiveCFindRSP(byte pid, string sopClass, ushort mid, ushort status, DataSet ds)
      {
         base.LogMessage(string.Concat(new object[] { "CFind Response: ", sopClass, ", ", status }));
         if (ds != null)
         {
            this.dataList.Add(ds);
         }
         if ((status != 0xff00) && (status != 0xff01))
         {
            base.SendReleaseRQ();
         }
      }

      public virtual void OnReceiveCGetRQ(byte pid, string sopClass, ushort mid, ushort prioirty, DataSet ds)
      {
         base.LogMessage("CGet Request: " + this.CallingEntityTitle);
         this.messageID = mid;
         this.thread = new Thread(new ParameterizedThreadStart(this.DoCGet));
         this.thread.Start(ds);
      }

      public virtual void OnReceiveCGetRSP(byte pid, string sopClass, ushort mid, ushort status, ushort rc, ushort cc, ushort fc, ushort wc, DataSet ds)
      {
         base.LogMessage(string.Concat(new object[] { "CGet Response: ", status, ", ", rc, ", ", cc, ", ", fc, ", ", wc }));
         this.messageID = rc;
         this.responseST = status;
         if (status != 0xff00)
         {
            base.SendReleaseRQ();
         }
      }

      public virtual void OnReceiveCMoveRQ(byte pid, string sopClass, ushort mid, ushort prioirty, string dest, DataSet ds)
      {
         base.LogMessage("CMove Request: " + dest);
         this.messageID = mid;
         this.Destination = dest;
         this.thread = new Thread(new ParameterizedThreadStart(this.DoCMove));
         this.thread.Start(ds);
      }

      public virtual void OnReceiveCMoveRSP(byte pid, string sopClass, ushort mid, ushort status, ushort rc, ushort cc, ushort fc, ushort wc, DataSet ds)
      {
         base.LogMessage(string.Concat(new object[] { "CMove Response: ", status, ", ", rc, ", ", cc, ", ", fc, ", ", wc }));
         this.messageID = rc;
         this.responseST = status;
         if (status != 0xff00)
         {
            base.SendReleaseRQ();
         }
      }

      public virtual void OnReceiveCStoreRQ(byte pid, string sopClass, ushort mid, ushort prioirty, string sopInstance, string origin, ushort oid, DataSet ds)
      {
         if (ds != null)
         {
            base.LogMessage("CStore Request: " + sopClass + ", " + sopInstance);
            ds.Dispose();
            this.SendCStoreRSP(pid, sopClass, mid, 0, sopInstance);
         }
         else
         {
            base.LogMessage("CStore Error: " + sopClass + ", " + sopInstance);
            this.SendCStoreRSP(pid, sopClass, mid, 0xc000, sopInstance);
         }
      }

      public virtual void OnReceiveCStoreRSP(byte pid, string sopClass, ushort mid, ushort status, string sopInstance)
      {
         base.LogMessage(string.Concat(new object[] { "CStore Response: ", sopClass, ", ", status, ", ", sopInstance, ", ", mid }));
         this.messageID = mid;
         this.responseST = status;
         if (this.thread == null)
         {
            if (mid >= this.DataCount)
            {
               base.SendReleaseRQ();
            }
            else
            {
               DataSet ds = (DataSet)this.dataList[mid];
               byte iD = pid;
               if (ds.SOPClassUID != sopClass)
               {
                  foreach (PresentationContext context in this.assoicate.PresentationContexts)
                  {
                     if (context.AbstractSyntax == ds.SOPClassUID)
                     {
                        iD = context.ID;
                        break;
                     }
                  }
               }
               this.SendCStoreRQ(iD, ds.SOPClassUID, (ushort)(mid + 1), 0, ds.SOPInstanceUID, this.Originator, 1, ds);
            }
         }
      }

      public override void OnReceiveData(byte pid, byte mch, byte[] data)
      {
         base.OnReceiveData(pid, mch, data);
         this.LastReceiveTime = DateTime.Now;
         string sopClass = "";
         ushort num = 0;
         ushort mid = 0;
         string dest = "";
         ushort prioirty = 0;
         ushort num4 = 0x101;
         ushort status = 0;
         string sopInstance = "";
         ushort rc = 0;
         ushort cc = 0;
         ushort fc = 0;
         ushort wc = 0;
         string origin = "";
         ushort oid = 0;
         if ((mch & 1) == 1)
         {
            if (!((this.dataStream != null) && this.dataStream.CanWrite))
            {
               this.dataStream = new MemoryStream();
            }
            if (data != null)
            {
               this.dataStream.Write(data, 0, data.Length);
            }
            if ((mch & 2) == 2)
            {
               this.dataStream.Position = 0L;
               this.commandSet = DataSet.FromStream(this.dataStream, "1.2.840.10008.1.2");
               this.dataStream.Close();
               this.dataStream = null;
               if (this.commandSet == null)
               {
                  base.LogMessage("Invalid Command Set.");
                  base.SendAbort(2, 6);
                  base.Close();
               }
               else
               {
                  foreach (DataElement element in this.commandSet)
                  {
                     switch (element.ElementNumber)
                     {
                        case 0x110:
                        case 0x120:
                           mid = element.ToUInt16();
                           break;

                        case 0x600:
                           dest = element.ToString();
                           break;

                        case 2:
                           sopClass = element.ToString();
                           break;

                        case 0x100:
                           num = element.ToUInt16();
                           break;

                        case 0x700:
                           prioirty = element.ToUInt16();
                           break;

                        case 0x800:
                           num4 = element.ToUInt16();
                           break;

                        case 0x900:
                           status = element.ToUInt16();
                           break;

                        case 0x1020:
                           rc = element.ToUInt16();
                           break;

                        case 0x1021:
                           cc = element.ToUInt16();
                           break;

                        case 0x1022:
                           fc = element.ToUInt16();
                           break;

                        case 0x1023:
                           wc = element.ToUInt16();
                           break;

                        case 0x1000:
                           sopInstance = element.ToString();
                           break;

                        case 0x1030:
                           origin = element.ToString();
                           break;

                        case 0x1031:
                           oid = element.ToUInt16();
                           break;
                     }
                  }
                  switch (num)
                  {
                     case 0x20:
                     case 0x21:
                     case 1:
                     case 0x10:
                        return;

                     case 0x30:
                        this.OnReceiveCEchoRQ(pid, sopClass, mid);
                        return;

                     case 0x8001:
                        this.OnReceiveCStoreRSP(pid, sopClass, mid, status, sopInstance);
                        return;

                     case 0x8010:
                        if (num4 == 0x101)
                        {
                           this.OnReceiveCGetRSP(pid, sopClass, mid, status, rc, cc, fc, wc, null);
                        }
                        return;

                     case 0x8020:
                        if (num4 == 0x101)
                        {
                           this.OnReceiveCFindRSP(pid, sopClass, mid, status, null);
                        }
                        return;

                     case 0x8021:
                        if (num4 == 0x101)
                        {
                           this.OnReceiveCMoveRSP(pid, sopClass, mid, status, rc, cc, fc, wc, null);
                        }
                        return;

                     case 0x8030:
                        this.OnReceiveCEchoRSP(pid, sopClass, mid, status);
                        return;
                  }
                  base.LogMessage("Unexpected Command: " + num);
                  base.SendAbort(2, 5);
               }
            }
         }
         else if (this.commandSet == null)
         {
            base.LogMessage("Unexpected Data.");
            base.SendAbort(2, 2);
            base.Close();
         }
         else
         {
            DataSet set;
            string name = "";
            foreach (DataElement element in this.commandSet)
            {
               switch (element.ElementNumber)
               {
                  case 0x110:
                  case 0x120:
                     mid = element.ToUInt16();
                     break;

                  case 0x600:
                     dest = element.ToString();
                     break;

                  case 2:
                     sopClass = element.ToString();
                     break;

                  case 0x100:
                     num = element.ToUInt16();
                     break;

                  case 0x700:
                     prioirty = element.ToUInt16();
                     break;

                  case 0x800:
                     num4 = element.ToUInt16();
                     break;

                  case 0x900:
                     status = element.ToUInt16();
                     break;

                  case 0x1020:
                     rc = element.ToUInt16();
                     break;

                  case 0x1021:
                     cc = element.ToUInt16();
                     break;

                  case 0x1022:
                     fc = element.ToUInt16();
                     break;

                  case 0x1023:
                     wc = element.ToUInt16();
                     break;

                  case 0x1000:
                     sopInstance = element.ToString();
                     break;

                  case 0x1030:
                     origin = element.ToString();
                     break;

                  case 0x1031:
                     oid = element.ToUInt16();
                     break;
               }
            }
            if ((this.dataStream == null) || !this.dataStream.CanWrite)
            {
               if (num != 1)
               {
                  this.dataStream = new MemoryStream();
               }
               else
               {
                  if (sopInstance == "")
                  {
                     sopInstance = DateTime.Now.ToString("yyyyMMddHHmmss");
                  }
                  set = null;
                  foreach (PresentationContext context in this.assoicate.PresentationContexts)
                  {
                     if (context.ID == pid)
                     {
                        set = new DataSet(sopClass, sopInstance, (string)context.TransferSyntaxes[0]);
                        break;
                     }
                  }
                  if (set == null)
                  {
                     set = new DataSet(sopClass, sopInstance, "");
                  }

                  //천승훈,윤현식 2018-05-11 수정

                  string[] stduid = Regex.Split(sopInstance, "[.]");

						//this.storePath = this.Destination + @"\" + stduid[0] + stduid[1] + stduid[2] + stduid[3] + stduid[4] + stduid[5] + stduid[6];

						this.storePath = this.Destination + @"\" + Filename;

						if (!Directory.Exists(this.storePath))
						{
							Directory.CreateDirectory(this.storePath);
						}

						name = this.storePath + @"\" + sopInstance;
						set.Save(name);
                  this.dataStream = new FileStream(name, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                  this.dataStream.Seek(0L, SeekOrigin.End);
               }
            }
            if (data != null)
            {
               this.dataStream.Write(data, 0, data.Length);
            }
            if ((mch & 2) == 2)
            {
               string tuid = "1.2.840.10008.1.2";
               foreach (PresentationContext context in this.assoicate.PresentationContexts)
               {
                  if (context.ID == pid)
                  {
                     tuid = (string)context.TransferSyntaxes[0];
                     break;
                  }
               }
               set = null;
               if (num == 1)
               {
                  name = ((FileStream)this.dataStream).Name;
                  this.dataStream.Close();
                  set = DataSet.FromFile(name);
               }
               else
               {
                  this.dataStream.Position = 0L;
                  set = DataSet.FromStream(this.dataStream, sopClass, tuid);
                  this.dataStream.Close();
               }
               switch (num)
               {
                  case 1:
                     this.OnReceiveCStoreRQ(pid, sopClass, mid, prioirty, sopInstance, origin, oid, set);
                     break;

                  case 0x10:
                     this.OnReceiveCGetRQ(pid, sopClass, mid, prioirty, set);
                     break;

                  case 0x20:
                     this.OnReceiveCFindRQ(pid, sopClass, mid, prioirty, set);
                     break;

                  case 0x21:
                     this.OnReceiveCMoveRQ(pid, sopClass, mid, prioirty, dest, set);
                     break;

                  case 0x8010:
                     this.OnReceiveCGetRSP(pid, sopClass, mid, status, rc, cc, fc, wc, set);
                     break;

                  case 0x8020:
                     this.OnReceiveCFindRSP(pid, sopClass, mid, status, set);
                     break;

                  case 0x8021:
                     this.OnReceiveCMoveRSP(pid, sopClass, mid, status, rc, cc, fc, wc, set);
                     break;

                  default:
                     base.LogMessage("Unexpected Command: " + num);
                     base.SendAbort(2, 5);
                     return;
               }
               this.dataStream = null;
               this.commandSet = null;
            }
         }
      }

      public override void OnReceiveReleaseRP()
      {
         base.OnReceiveReleaseRP();
         base.LogMessage("Released: " + this.CalledEntityTitle);
         base.Close();
      }

      public override void OnReceiveReleaseRQ()
      {
         base.OnReceiveReleaseRQ();
         base.LogMessage("Release: " + this.CallingEntityTitle);
         base.SendReleaseRP();
         base.Close();
      }

      public override void OnReceiveUnknown(byte type, byte[] buffer)
      {
         base.OnReceiveUnknown(type, buffer);
         base.LogMessage("Unknown PDU Type: " + type);
         base.Close();
      }

      public void SendCCancelRQ(byte pid, ushort mid)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0xfff);
         set.Add(de);
         de = new DataElement(0x120, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0x101);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.Close();
      }

      public void SendCEchoRQ(byte pid, string sopClass, ushort mid)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x30);
         set.Add(de);
         de = new DataElement(0x110, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0x101);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.Close();
      }

      public void SendCEchoRSP(byte pid, string sopClass, ushort mid, ushort status)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x8030);
         set.Add(de);
         de = new DataElement(0x120, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0x101);
         set.Add(de);
         de = new DataElement(0x900, "US", 2);
         de.SetValue(status);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.Close();
      }

      public void SendCFindRQ(byte pid, string sopCalss, ushort mid, ushort priority, DataSet ds)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopCalss);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x20);
         set.Add(de);
         de = new DataElement(0x110, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x700, "US", 2);
         de.SetValue(priority);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.SetLength(0L);
         ds.Save(output);
         base.SendData(pid, 2, output.ToArray());
         output.Close();
         this.dataList.Clear();
      }

      public void SendCFindRSP(byte pid, string sopClass, ushort mid, ushort status, DataSet ds)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x8020);
         set.Add(de);
         de = new DataElement(0x120, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         if (ds != null)
         {
            de.SetValue((ushort)0);
         }
         else
         {
            de.SetValue((ushort)0x101);
         }
         set.Add(de);
         de = new DataElement(0x900, "US", 2);
         de.SetValue(status);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.SetLength(0L);
         if (ds != null)
         {
            ds.Save(output);
            base.SendData(pid, 2, output.ToArray());
         }
         output.Close();
      }

      public void SendCGetRQ(byte pid, string sopClass, ushort mid, ushort priority, DataSet ds)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x10);
         set.Add(de);
         de = new DataElement(0x110, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x700, "US", 2);
         de.SetValue(priority);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0x102);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.SetLength(0L);
         ds.Save(output);
         base.SendData(pid, 2, output.ToArray());
         output.Close();
      }

      public void SendCGetRSP(byte pid, string sopClass, ushort mid, ushort status, ushort rc, ushort cc, ushort fc, ushort wc, DataSet ds)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x8010);
         set.Add(de);
         de = new DataElement(0x120, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         if (ds != null)
         {
            de.SetValue((ushort)0);
         }
         else
         {
            de.SetValue((ushort)0x101);
         }
         set.Add(de);
         de = new DataElement(0x900, "US", 2);
         de.SetValue(status);
         set.Add(de);
         de = new DataElement(0x1020, "US", 2);
         de.SetValue(rc);
         set.Add(de);
         de = new DataElement(0x1021, "US", 2);
         de.SetValue(cc);
         set.Add(de);
         de = new DataElement(0x1022, "US", 2);
         de.SetValue(fc);
         set.Add(de);
         de = new DataElement(0x1023, "US", 2);
         de.SetValue(wc);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.SetLength(0L);
         if (ds != null)
         {
            ds.Save(output);
            base.SendData(pid, 2, output.ToArray());
         }
         output.Close();
      }

      public void SendCMoveRQ(byte pid, string sopClass, ushort mid, ushort priority, string dest, DataSet ds)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x21);
         set.Add(de);
         de = new DataElement(0x110, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x700, "US", 2);
         de.SetValue(priority);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0x102);
         set.Add(de);
         de = new DataElement(0x600, "AE", 0);
         de.SetValue(dest);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.SetLength(0L);
         ds.Save(output);
         base.SendData(pid, 2, output.ToArray());
         output.Close();
      }

      public void SendCMoveRSP(byte pid, string sopClass, ushort mid, ushort status, ushort rc, ushort cc, ushort fc, ushort wc, DataSet ds)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x8021);
         set.Add(de);
         de = new DataElement(0x120, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         if (ds != null)
         {
            de.SetValue((ushort)0);
         }
         else
         {
            de.SetValue((ushort)0x101);
         }
         set.Add(de);
         de = new DataElement(0x900, "US", 2);
         de.SetValue(status);
         set.Add(de);
         de = new DataElement(0x1020, "US", 2);
         de.SetValue(rc);
         set.Add(de);
         de = new DataElement(0x1021, "US", 2);
         de.SetValue(cc);
         set.Add(de);
         de = new DataElement(0x1022, "US", 2);
         de.SetValue(fc);
         set.Add(de);
         de = new DataElement(0x1023, "US", 2);
         de.SetValue(wc);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.SetLength(0L);
         if (ds != null)
         {
            ds.Save(output);
            base.SendData(pid, 2, output.ToArray());
         }
         output.Close();
      }

      public void SendCStoreRQ(byte pid, string sopClass, ushort mid, ushort priority, string sopInstance, string origin, ushort oid, DataSet ds)
      {
         FileStream output = null;
         byte[] buffer;
         string uid = "1.2.840.10008.1.2";
         foreach (PresentationContext context in this.assoicate.PresentationContexts)
         {
            if (context.ID == pid)
            {
               uid = (string)context.TransferSyntaxes[0];
               if ((uid != ds.TransferSyntaxUID) && (ds.TransferSyntaxUID != "1.2.840.10008.1.2.1"))
               {
                  output = new FileStream(this.Destination + @"\" + sopInstance, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                  ds.Save(output, uid);
                  output.Position = 0L;
                  base.LogMessage("Convert Image from " + ds.TransferSyntaxUID + " to " + uid);
               }
               break;
            }
         }
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)1);
         set.Add(de);
         de = new DataElement(0x110, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de.SetValue(priority);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0);
         set.Add(de);
         de = new DataElement(0x1000, "UI", 0);
         de.SetValue(sopInstance);
         set.Add(de);
         de = new DataElement(0x1030, "AE", 0);
         de.SetValue(origin);
         set.Add(de);
         de = new DataElement(0x1031, "US", 2);
         de.SetValue(oid);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream stream2 = new MemoryStream();
         set.Save(stream2);
         base.SendData(pid, 3, stream2.ToArray());
         stream2.Close();
         int count = 0x3ff4;
         if ((this.assoicate.MaximumLength > 12) && (this.assoicate.MaximumLength < 0x4000))
         {
            count = ((int)this.assoicate.MaximumLength) - 12;
         }
         if (output != null)
         {
            buffer = new byte[count];
            while (output.Position < output.Length)
            {
               if ((output.Position + count) > output.Length)
               {
                  buffer = new byte[output.Length - output.Position];
               }
               output.Read(buffer, 0, buffer.Length);
               base.SendData(pid, 0, buffer);
            }
            base.SendData(pid, 2, null);
            output.Close();
            File.Delete(output.Name);
         }
         else
         {
            byte[] data = new byte[count];
            int destinationIndex = 0;
            foreach (DataElement element2 in ds)
            {
               buffer = element2.ToArray(uid);
               int index = 0;
               while (index < buffer.Length)
               {
                  data[destinationIndex++] = buffer[index++];
                  if (destinationIndex == count)
                  {
                     base.SendData(pid, 0, data);
                     destinationIndex = 0;
                  }
               }
               if (((element2.Length >= 0x10000) && (element2.Length < uint.MaxValue)) && (buffer.Length <= 12))
               {
                  Array.Copy(element2.GetValue(0, count - destinationIndex, 0), 0, data, destinationIndex, count - destinationIndex);
                  base.SendData(pid, 0, data);
                  for (index = count - destinationIndex; index < element2.Length; index += count)
                  {
                     buffer = element2.GetValue(index, count, 0);
                     if (buffer == null)
                     {
                        destinationIndex = 0;
                        break;
                     }
                     if (buffer.Length < count)
                     {
                        Array.Copy(buffer, 0, data, 0, buffer.Length);
                        destinationIndex = buffer.Length;
                        break;
                     }
                     base.SendData(pid, 0, buffer);
                  }
               }
            }
            if (destinationIndex > 0)
            {
               buffer = new byte[destinationIndex];
               Array.Copy(data, 0, buffer, 0, destinationIndex);
               base.SendData(pid, 0, buffer);
            }
            base.SendData(pid, 2, null);
         }
      }

      public void SendCStoreRSP(byte pid, string sopClass, ushort mid, ushort status, string sopInstance)
      {
         DataSet set = new DataSet();
         DataElement de = new DataElement(2, "UI", 0);
         de.SetValue(sopClass);
         set.Add(de);
         de = new DataElement(0x100, "US", 2);
         de.SetValue((ushort)0x8001);
         set.Add(de);
         de = new DataElement(0x120, "US", 2);
         de.SetValue(mid);
         set.Add(de);
         de = new DataElement(0x800, "US", 2);
         de.SetValue((ushort)0x101);
         set.Add(de);
         de = new DataElement(0x900, "US", 2);
         de.SetValue(status);
         set.Add(de);
         de = new DataElement(0x1000, "UI", 0);
         de.SetValue(sopInstance);
         set.Add(de);
         de = new DataElement(0, "UL", 4);
         de.SetValue((uint)set.Length);
         set.Insert(0, de);
         MemoryStream output = new MemoryStream();
         set.Save(output);
         base.SendData(pid, 3, output.ToArray());
         output.Close();
      }

      public string CalledEntityTitle
      {
         get
         {
            return this.assoicate.CalledEntityTitle;
         }
         set
         {
            this.assoicate.CalledEntityTitle = value;
         }
      }

      public string CallingEntityTitle
      {
         get
         {
            return this.assoicate.CallingEntityTitle;
         }
         set
         {
            this.assoicate.CallingEntityTitle = value;
         }
      }

      public int DataCount
      {
         get
         {
            return this.dataList.Count;
         }
      }

      public uint MaximumLength
      {
         get
         {
            return this.assoicate.MaximumLength;
         }
         set
         {
            this.assoicate.MaximumLength = value;
         }
      }

      public uint Status
      {
         get
         {
            return ((uint)((this.messageID * 0x10000) + this.responseST));
         }
      }
   }
}


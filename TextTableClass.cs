using SQLToolsClass;
using System;
using System.Collections.Generic;
using System.IO;

namespace TextTableClass
{
    internal class CRecord
    {
        public int Id;
        public string IndexName;
        public List<string> FieldsList;

        internal void Create()
        {
            FieldsList = new List<string>();
        }
    }

    internal class TextTable
    {
        public const char TAB = (char)9; //'\t';
        public List<string> FieldNames;
        public List<CRecord> Records;
        public string Name;
        public string Source;
        public string IndexName;
        public bool Indexed;
        public bool Sorted;
        public int Row;

        public void Create()
        {
            FieldNames = new List<string>();
            Records = new List<CRecord>();
        }

        public void LoadTextTable(string tableName, string sourceFile, string indexName)
        {
            Create();
            Name = tableName;
            Source = sourceFile;
            Load();
            if (indexName != "")
            {
                Index(indexName);
            }
        }

        private int GetFieldIndex(string fieldName)
        {
            int result = FieldNames.IndexOf(fieldName.ToUpper());
            if (result < 0)
                throw new Exception("Cannot locate field " + fieldName + " from table " + Name);
            return result;
        }

        internal void First()
        {
            if (Records.Count > 0)
            {
                Row = 0;
            }
            else
            {
                Row = -1;
            }
        }

        internal void Next()
        {
            if (Row < Records.Count - 1)
            {
                ++Row;
            }
            else
            {
                Row = -1;
            }
        }

        internal void SetId(int value)
        {
            if ((Row < 0) || (Row > Records.Count - 1))
            {
                throw new Exception("Index out of bounds");
            }
            Records[Row].Id = value;
        }

        internal int GetId()
        {
            if ((Row < 0) || (Row > Records.Count - 1))
            {
                throw new Exception("Index out of bounds");
            }
            return Records[Row].Id;
        }

        internal string GetString(string fieldName)
        {
            if ((Row < 0) || (Row > Records.Count - 1))
            {
                throw new Exception("Index out of bounds");
            }
            int index = GetFieldIndex(fieldName.ToUpper());
            try
            {
                return Records[Row].FieldsList[index];
            }
            catch
            {
                throw new Exception("Error reading field " + fieldName + " from table " + Name + " for row " + (Row + 1));
            }

        }

        internal int GetInteger(string fieldName)
        {
            return Tools.StringToInteger(GetString(fieldName));
        }

        internal double GetFloat(string fieldName)
        {
            return Tools.StringToFloat(GetString(fieldName));
        }

        internal int GetDate(string fieldName)
        {
            return Tools.StringToDate(GetString(fieldName));
        }

        internal DateTime GetDateTime(string fieldName)
        {
            return Tools.StringToDateTime(GetString(fieldName));
        }

        internal int GetMoney(string fieldName)
        {
            return (GetInteger(fieldName));
        }

        internal bool GetBoolean(string fieldName)
        {
            return Tools.StringToBool(GetString(fieldName));
        }

        internal double GetPercent(string fieldName)
        {
            return Tools.StringToPercent(GetString(fieldName));
        }

        internal void Load()
        {
            if (File.Exists(Source))
            {
                try
                {
                    string[] arrstr = File.ReadAllLines(Source);
                    bool bHeading = true;
                    foreach (string strLine in arrstr)
                    {
                        if (bHeading)
                        {
                            string[] arrStringHeading = strLine.Split(TAB);
                            foreach (string strHeading in arrStringHeading)
                            {
                                FieldNames.Add(strHeading.ToUpper());
                            }
                        }
                        else
                        {
                            string[] arrStringRecord = strLine.Split(TAB);
                            CRecord rec = new CRecord();
                            rec.Create();
                            try
                            {
                                foreach (string l_strRecord in arrStringRecord)
                                {
                                    rec.FieldsList.Add(l_strRecord);
                                }
                                rec.IndexName = "";
                                Records.Add(rec);
                            }
                            catch { }
                        }
                        bHeading = false;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error loading file: " + Name + ".txt" + ": " + ex.Message);
                }
                Row = -1;
            }
            else
            {
                throw new Exception("File does not exist: " + Name + ".txt");
            }
        }

        internal void Index(string fieldNames)
        {
            try
            {
                fieldNames = fieldNames.ToUpper();
                Row = -1;
                if (IndexName == fieldNames)
                {
                    return;
                }
                Indexed = false;
                foreach (CRecord t in Records)
                {
                    t.IndexName = "";
                }
                IndexName = "";
                if (fieldNames == "")
                {
                    foreach (CRecord t in Records)
                    {
                        t.IndexName = t.Id.ToString();
                    }
                }
                else
                {
                    IndexName = fieldNames;
                    bool isFirstField = true;
                    string[] fieldNameArray = fieldNames.Split(TAB);
                    try
                    {
                        foreach (string fieldName in fieldNameArray)
                        {
                            int fieldIndex = GetFieldIndex(fieldName);
                            if (isFirstField)
                            {
                                foreach (CRecord record in Records)
                                {
                                    record.IndexName = record.FieldsList[fieldIndex];
                                }
                                isFirstField = false;
                            }
                            else
                            {
                                foreach (CRecord record in Records)
                                {
                                    record.IndexName = record.IndexName + TAB + record.FieldsList[fieldIndex];
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        foreach (CRecord record in Records)
                        {
                            record.IndexName = "";
                        }
                        IndexName = "";
                        throw new Exception("Failed to index " + Name + " on " + fieldNames + ": " + exception.Message);
                    }
                }
                Records.Sort((record1, record2) => String.Compare(record1.IndexName, record2.IndexName, StringComparison.Ordinal));
                Sorted = true;
                Indexed = true;
            }
            catch (Exception exception)
            {
                throw new Exception("Index failed: " + exception.Message);
            }
        }

        internal int Count()
        {
            int result = -1;
            if (Records != null)
                result = Records.Count;
            return result;
        }

        internal bool Locate(string field)
        {
            Row = Records.FindIndex(rec => rec.IndexName == field);
            while ((Row > 0) && (Records[Row].IndexName == Records[Row - 1].IndexName))
            {
                --Row;
            }
            return (Row != -1);
        }

        internal bool LocateId(int Id)
        {
            Row = Records.FindIndex(rec => rec.Id == Id);
            while ((Row > 0) && (Records[Row].Id == Records[Row - 1].Id))
            {
                --Row;
            }
            return (Row != -1);
        }

        internal bool EOF()
        {
            //Check if we have reached the end of the file
            return Row < 0;
        }

        internal bool FieldExists(string fieldName)
        {
            return FieldNames.Contains(fieldName.ToUpper().Trim());
        }

    }
}
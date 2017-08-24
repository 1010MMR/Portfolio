using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;

class CreateNewAssetbundles3 {
    [MenuItem("MagiBuilder3/PatchClear/CleanCache")]
    static void CleanCache() {
        Caching.CleanCache();

        string dbPath = Application.persistentDataPath + "/rustyhearts.db";
        MagiDebug.Log(dbPath);

        File.Delete(dbPath);
    }

    [MenuItem("MagiBuilder3/Patch/KR/Android")]
    static void ExecuteAndroid() {
        ExecutePatch("KR", "Android");
    }

    [MenuItem("MagiBuilder3/Patch/KR/iOS")]
    static void ExecuteiOS() {
        ExecutePatch("KR", "iPhone");
    }

    public class CAssetBundleCheck {
        public uint count;
        public ulong size;
        public ulong crc;
    }

    static string GetExcludeFileName_Assets(string pathName) {
        bool assets = false;
        string[] pathSplit = pathName.Split(new System.Char[] { '/' });
        string excludeFileName = "";
        for( int i = 0; i < pathSplit.Length - 1; ++i ) {
            if( pathSplit[i].ToLower() == "assets" ) {
                assets = true;
                continue;
            }
            if( assets == false )
                continue;
            excludeFileName += pathSplit[i];
            excludeFileName += '/';
        }
        return excludeFileName;
    }

    static void GetPrefabsFolderFileNameList(out List<string> fileList) {
        fileList = new List<string>();

        int currentDirectoryLength = System.Environment.CurrentDirectory.Length + 1;
        string prefabsPath = System.Environment.CurrentDirectory + @"/Assets/Prefabs";
        MagiDebug.Log(prefabsPath);

        string[] files = Directory.GetFiles(prefabsPath, "*.*", SearchOption.AllDirectories);
        foreach( var file in files ) {
            if( Path.GetExtension(file).ToLower() == ".meta" )
                continue;
            if( Path.GetExtension(file).ToLower() == ".php" )
                continue;
            if( Path.GetExtension(file).ToLower() == ".csv" )
                continue;

            if( file.ToLower().Contains("nopatch") == true ) {
                MagiDebug.Log(file);
                continue;
            }

            string temp = file.Remove(0, currentDirectoryLength);
            MagiDebug.Log(temp);
            fileList.Add(temp);
        }
    }

    static void ExecutePatch(string local, string target) {
        //        System.DateTime nowDateTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");
        System.DateTime nowDateTime = System.DateTime.Now;

        BuildTarget buildTarget;
        string targetPath = "";
        switch( target ) {
            case "Android": {
                    targetPath = "android/";
                    buildTarget = BuildTarget.Android;
                }
                break;
            case "iPhone": {
                    targetPath = "iphone/";
                    buildTarget = BuildTarget.iPhone;
                }
                break;
            default:
                return;
        }

        string localPath = local.ToLower() + "/";

        //patch revision info 로딩.
        int lastRevision = 0;
        string version = "";
        string infoPatchRevisionInfo = GetAssetbundlePath(buildTarget) + localPath + targetPath + @"patchRevisionInfo.txt";
        if( File.Exists(infoPatchRevisionInfo) == true ) {
            string[] infoPatchRevision = File.ReadAllLines(infoPatchRevisionInfo);
            if( infoPatchRevision.Length <= 2 ) {
                MagiDebug.LogError(string.Format("{0} file error", infoPatchRevisionInfo));
                return;
            }

            lastRevision = System.Int32.Parse(infoPatchRevision[0]);
            version = infoPatchRevision[1];
        }

        else {
            version = nowDateTime.ToString("yyyy-MM-dd_HH:mm:ss");
        }

        lastRevision++;

        EditorUtility.UnloadUnusedAssets();

        string lastAssetBundleCheckInfoFileName = "AssetBundleCheckInfo3_" + target + ".txt";
        //        Object[] listObject = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

        List<string> fileList;
        GetPrefabsFolderFileNameList(out fileList);

        //pathlist 작업.
        string newAssetbundlePath = GetAssetbundlePath(buildTarget) + localPath + targetPath + lastRevision.ToString() + "/";
        string lastAssetBundleCheckInfoPathName = System.Environment.CurrentDirectory + @"/Assets/Prefabs/" + lastAssetBundleCheckInfoFileName;
        string errorListPath = System.Environment.CurrentDirectory + @"/Assets/Prefabs/" + "AssetBundleError.txt";

        Dictionary<string, CAssetBundleCheck> lastAssetBundleCheckInfo = new Dictionary<string, CAssetBundleCheck>();
        ReadAssetBundleCheckInfo(out lastAssetBundleCheckInfo, lastAssetBundleCheckInfoPathName);
        Dictionary<string, CAssetBundleCheck> assetBundleCheckInfo = new Dictionary<string, CAssetBundleCheck>();

        Dictionary<string, List<Object>> hashAssetBundle = new Dictionary<string, List<Object>>();
        //        for (int i = 0; i < listObject.Length; ++i)
        //        {
        //            Object o = listObject[i];

        List<ePatchInfo> newPathInfoAssetBundle = new List<ePatchInfo>();
        ePatchInfo currentPatchInfo = new ePatchInfo();

        for( int i = 0; i < fileList.Count; ++i ) {
            Object o = null;
            try {
                o = AssetDatabase.LoadMainAssetAtPath(fileList[i]);
            } catch {
                MagiDebug.LogError(string.Format("AssetDatabase.LoadMainAssetAtPath error, {0}", fileList[i]));
                continue;
            }

            if( o == null ) {
                MagiDebug.LogError("AssetDatabase.LoadMainAssetAtPath == false, " + fileList[i]);
                continue;
            }
            //Object o = fileList[i];

            //            MagiDebug.Log(o);

            string assetPath = AssetDatabase.GetAssetPath(o).ToLower();
            if( assetPath.Contains("assets/prefabs/") == false )
                continue;
            if( assetPath.Contains("assets/prefabs/pathinfo") == true )
                continue;
            if( assetPath.Contains("assets/prefabs/patchrevisioninfo.txt") == true )
                continue;

            float s = (float)i / fileList.Count;
            EditorUtility.DisplayProgressBar("Make AssetBundleCheckInfo", assetPath, s);

            string assetBundleName = GetAssetBundleName(assetPath);
            if( assetBundleName == "" )
                continue;

            if( assetBundleCheckInfo.ContainsKey(assetBundleName) == true ) {
                CAssetBundleCheck temp = assetBundleCheckInfo[assetBundleName];
                GetAssetBundleCheck(o, ref temp);
            }
            else {
                CAssetBundleCheck temp = new CAssetBundleCheck();
                GetAssetBundleCheck(o, ref temp);
                assetBundleCheckInfo.Add(assetBundleName, temp);
            }

            if( hashAssetBundle.ContainsKey(assetBundleName) == true ) {
                hashAssetBundle[assetBundleName].Add(o);
            }
            else {
                var listO = new List<Object>();
                listO.Add(o);
                hashAssetBundle.Add(assetBundleName, listO);
            }

            System.GC.Collect();
            for( int n = 0; n < System.GC.MaxGeneration; ++n ) {
                System.GC.Collect(n, System.GCCollectionMode.Forced);
            }
            System.GC.WaitForPendingFinalizers();
            EditorUtility.UnloadUnusedAssets();
        }
        EditorUtility.ClearProgressBar();

        if( assetBundleCheckInfo.Count == 0 )
            return;

        string buildAssetBundle = System.Environment.CurrentDirectory + @"/Assets/Prefabs/" + "BuildAssetBundle.txt";

        int c = 0;
        uint crc = 0;
        foreach( var p in hashAssetBundle ) {
            c++;
            float s = (float)c / hashAssetBundle.Count;
            EditorUtility.DisplayProgressBar("BuildAssetBundle", p.Key, s);

            CAssetBundleCheck abc;
            if( assetBundleCheckInfo.TryGetValue(p.Key, out abc) == false )
                continue;

            if( lastAssetBundleCheckInfo.ContainsKey(p.Key) == true ) {
                CAssetBundleCheck lastAbc;
                lastAssetBundleCheckInfo.TryGetValue(p.Key, out lastAbc);
                if( abc.count == lastAbc.count && abc.crc == lastAbc.crc && abc.size == lastAbc.size ) {
                    continue;
                }
            }

            string path = newAssetbundlePath + p.Key + ".assetbundle";
            MagiDebug.Log(path);

            if( !Directory.Exists(newAssetbundlePath) )
                Directory.CreateDirectory(newAssetbundlePath);

            File.WriteAllText(buildAssetBundle, path);

            BuildPipeline.PushAssetDependencies();

            if( BuildPipeline.BuildAssetBundle(p.Value[0], p.Value.ToArray(), path, out crc, BuildAssetBundleOptions.CollectDependencies, buildTarget) == false ) {
                MagiDebug.LogError("BuildPipeline.BuildAssetBundle == false, " + path);
            }

            BuildPipeline.PopAssetDependencies();

            currentPatchInfo = new ePatchInfo();
            currentPatchInfo.key = p.Key;
            currentPatchInfo.version = lastRevision;
            currentPatchInfo.crc = crc;

            newPathInfoAssetBundle.Add(currentPatchInfo);
        }

        EditorUtility.ClearProgressBar();

        // 패치할 파일이 없으면 pass
        if( newPathInfoAssetBundle.Count == 0 )
            return;

        //revision 버젼 저장.
        List<ePatchInfo> finalPatchInfo = new List<ePatchInfo>();
        if( File.Exists(infoPatchRevisionInfo) == true ) {
            string[] infoPatchRevision = File.ReadAllLines(infoPatchRevisionInfo);
            if( infoPatchRevision.Length <= 2 ) {
                MagiDebug.LogError(string.Format("{0} file error", infoPatchRevisionInfo));
                return;
            }

            ePatchInfo newPatchInfo;
            ePatchInfo patchInfo;
            string[] szAssetBundleInfo;
            for( int i = 2; i < infoPatchRevision.Length; i++ ) {
                szAssetBundleInfo = infoPatchRevision[i].Split(',');

                if( szAssetBundleInfo.Length < 3 )
                    continue;

                patchInfo = new ePatchInfo();
                patchInfo.key = szAssetBundleInfo[0];
                patchInfo.version = System.Int32.Parse(szAssetBundleInfo[1]);
                patchInfo.crc = System.UInt32.Parse(szAssetBundleInfo[2]);

                newPatchInfo = newPathInfoAssetBundle.Find(delegate(ePatchInfo a) {
                    return a.key.Equals(patchInfo.key);
                });

                if( newPatchInfo != null )
                    finalPatchInfo.Add(newPatchInfo);
                else
                    finalPatchInfo.Add(patchInfo);
            }
        }

        else {
            finalPatchInfo.AddRange(newPathInfoAssetBundle);
        }

        finalPatchInfo.Sort(delegate(ePatchInfo a, ePatchInfo b) {
            return a.key.CompareTo(b.key);
        });

        List<string> szFinalPatchInfo = new List<string>();
        for( int i = 0; i < finalPatchInfo.Count; i++ ) {
            szFinalPatchInfo.Add(string.Format("{0},{1},{2}", finalPatchInfo[i].key, finalPatchInfo[i].version.ToString(), finalPatchInfo[i].crc.ToString()));
        }

        List<string> revisionInfo = new List<string>();
        revisionInfo.Add(lastRevision.ToString());
        revisionInfo.Add(version.ToString());

        revisionInfo.AddRange(szFinalPatchInfo);
        
        if( File.Exists(infoPatchRevisionInfo) == true )
            File.Delete(infoPatchRevisionInfo);

        File.WriteAllLines(infoPatchRevisionInfo, revisionInfo.ToArray());

        WriteAssetBundleCheckInfo(assetBundleCheckInfo, lastAssetBundleCheckInfoPathName);

        MagiDebug.Log(System.DateTime.Now - nowDateTime);

        #region FTP unload작업.
        //{
        //    //            string ftpRoot = "app/rustyhearts/" + targetPath + lastRevision.ToString() + "/";
        //    string ftpRoot = localPath + targetPath + lastRevision.ToString() + "/";
        //    MagiFtp magiFtp = new MagiFtp("1.214.41.130", "rh_rpg", "z00games?!ftp");

        //    string ftpRoot2 = localPath + targetPath + lastRevision.ToString();
        //    magiFtp.MakeDirectory(ftpRoot2);

        //    for (int i = 0; i < pathInfoAssetBundle.Count; )
        //    {
        //        string ftpFile = ftpRoot + pathInfoAssetBundle[i] + ".assetbundle";
        //        string clientPath = newAssetbundlePath + pathInfoAssetBundle[i] + ".assetbundle";

        //        //                FtpDelete(magiFtp, ftpFile);
        //        CreateNewAssetbundles.FtpUpload(magiFtp, ftpFile, clientPath);

        //        float s = (float)i / pathInfoAssetBundle.Count;
        //        EditorUtility.DisplayProgressBar("Upload", clientPath, s);
        //        ++i;
        //    }
        //    EditorUtility.ClearProgressBar();

        //    // fileinfo.txt upload
        //    {
        //        string ftpDir = ftpRoot + Path.GetFileName(newFileInfoPathName);
        //        string clientPath = newFileInfoPathName;
        //        CreateNewAssetbundles.FtpUpload(magiFtp, ftpDir, clientPath);
        //    }

        //    // patchRevisionInfo.txt upload
        //    {
        //        string ftpDir = localPath + targetPath + "patchRevisionInfo.txt";
        //        string clientPath = infoPatchRevisionInfo;
        //        CreateNewAssetbundles.FtpUpload(magiFtp, ftpDir, clientPath);
        //    }
        //}
        #endregion
    }

    public static bool GetAssetBundleCheck(Object o, ref CAssetBundleCheck abc) {
        string collectDependenciesAssetPath = AssetDatabase.GetAssetPath(o).ToLower();

        Object[] dependencies;

        //        dependencies = new[] { o };
        if( o is GameObject ) {
            dependencies = EditorUtility.CollectDependencies(new[] { o });
        }
        else if( o is TextAsset || o is Texture2D || o is AudioClip ) {
            dependencies = new[] { o };
        }
        else {
            return false;
        }

        foreach( var od in dependencies ) {
            //if (od is TextAsset == false && od is GameObject == false && od is Texture2D == false && od is AudioClip == false)
            //{
            //    continue;
            //}

            string assetPath = AssetDatabase.GetAssetPath(od).ToLower();
            if( Path.GetExtension(assetPath) == ".cs" ) {
                MagiDebug.Log("GetAssetBundleCheck .cs - " + assetPath);
                continue;
            }

            var path = System.Environment.CurrentDirectory + "/" + assetPath;
            if( File.Exists(path) == false ) {
                continue;
            }

            uint crc;
            if( MakeCRC.Make(path, out crc) == true ) {
                abc.crc += crc;
            }
            FileInfo fi = new FileInfo(path);
            abc.size += (ulong)fi.Length;
            abc.count++;
        }
        dependencies = null;
        return true;
    }

    public static bool GetAssetBundleCheck(Object[] objects, out CAssetBundleCheck abc) {
        HashSet<string> hsFileList = new HashSet<string>();
        abc = new CAssetBundleCheck();

        int i = 0;
        foreach( var ob in objects ) {
            float s = (float)i / objects.Length;

            string collectDependenciesAssetPath = AssetDatabase.GetAssetPath(ob).ToLower();
            EditorUtility.DisplayProgressBar("CollectDependencies", collectDependenciesAssetPath, s);

            var dependencies = EditorUtility.CollectDependencies(new[] { ob });
            foreach( var o in dependencies ) {
                if( o is TextAsset == false && o is GameObject == false && o is Texture2D == false && o is AudioClip == false ) {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(o).ToLower();
                if( hsFileList.Contains(assetPath) == false ) {
                    //                    hsFileList.Add(assetPath);
                }
            }
            dependencies = null;
        }
        EditorUtility.ClearProgressBar();

        i = 0;
        abc.count = (uint)hsFileList.Count;
        foreach( var fileName in hsFileList ) {
            float s = (float)i / abc.count;
            EditorUtility.DisplayProgressBar("GetAssetBundleCheck", string.Format("Get asset bundle check {0}/{1}", i, abc.count), s);

            var path = System.Environment.CurrentDirectory + "/" + fileName;
            if( File.Exists(path) == false ) {
                ++i;
                continue;
            }

            uint crc;
            if( MakeCRC.Make(path, out crc) == true ) {
                abc.crc += crc;
            }
            FileInfo fi = new FileInfo(path);
            abc.size += (ulong)fi.Length;
            ++i;

            //            int totalMemory = System.GC.GetTotalMemory(true);
        }
        EditorUtility.ClearProgressBar();
        return true;
    }

    public static string GetAssetbundlePath(BuildTarget buildTarget) {
        if( buildTarget == BuildTarget.iPhone ) {
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/work/rustyhearts/patchWork/assetbundles3/";
        }
        return "c:/work/unity/rustyhearts/patchWork/assetbundles3/";
    }

    public static string GetExcludeFileName(string pathName) {
        bool prefabs = false;
        string[] pathSplit = pathName.ToLower().Split(new System.Char[] { '/' });
        string excludeFileName = "";
        for( int i = 0; i < pathSplit.Length - 1; ++i ) {
            if( pathSplit[i] == "prefabs" ) {
                prefabs = true;
                continue;
            }
            if( prefabs == false )
                continue;
            excludeFileName += pathSplit[i];
            excludeFileName += '/';
        }
        return excludeFileName;
    }

    public static string GetAssetBundleName(string pathName) {
        bool prefabs = false;
        string[] pathSplit = pathName.ToLower().Split(new System.Char[] { '/' });
        string assetBundleName = "";
        for( int i = 0; i < pathSplit.Length - 1; ++i ) {
            if( pathSplit[i] == "prefabs" ) {
                prefabs = true;
                continue;
            }
            if( prefabs == false )
                continue;
            assetBundleName += pathSplit[i];
            break;
        }
        return assetBundleName;
    }

    public static void GetMakeDirectoryList(out List<string> mdList, HashSet<string> directoryList) {
        mdList = new List<string>();
        var mdList2 = new HashSet<string>();

        foreach( var directory in directoryList ) {
            string[] pathSplit = directory.Split(new System.Char[] { '/' });
            string curPath = "";
            for( int i = 0; i < pathSplit.Length - 1; ++i ) {
                curPath += pathSplit[i];
                if( mdList2.Contains(curPath) == false ) {
                    mdList2.Add(curPath);
                    mdList.Add(curPath);
                }
                curPath += '/';
            }
        }
        mdList.Sort();
    }

    public static bool ReadAssetBundleCheckInfo(out Dictionary<string, CAssetBundleCheck> hashAssetBundleCheck, string filename) {
        hashAssetBundleCheck = new Dictionary<string, CAssetBundleCheck>();
        if( File.Exists(filename) == false ) {
            return false;
        }
        string[] pathFile = File.ReadAllLines(filename);

        char[] separators = new char[] { ',' };
        foreach( string file in pathFile ) {
            string[] assetBundleCheck = file.Split(separators);
            if( assetBundleCheck.Length != 4 ) {
                MagiDebug.LogError("assetBundleCheck.Length != 4");
                return false;
            }

            CAssetBundleCheck abc = new CAssetBundleCheck();
            abc.count = System.UInt32.Parse(assetBundleCheck[1], System.Globalization.NumberStyles.Integer);
            abc.size = System.UInt64.Parse(assetBundleCheck[2], System.Globalization.NumberStyles.Integer);
            abc.crc = System.UInt64.Parse(assetBundleCheck[3], System.Globalization.NumberStyles.Integer);

            hashAssetBundleCheck.Add(assetBundleCheck[0], abc);
        }
        return true;
    }

    public static bool WriteAssetBundleCheckInfo(Dictionary<string, CAssetBundleCheck> hashAssetBundleCheck, string filename) {
        if( File.Exists(filename) ) {
            File.Delete(filename);
        }

        List<string> lines = new List<string>();
        foreach( var one in hashAssetBundleCheck ) {
            string line = string.Format("{0},{1},{2},{3}", one.Key, one.Value.count, one.Value.size, one.Value.crc);
            lines.Add(line);
        }
        lines.Sort();
        File.WriteAllLines(filename, lines.ToArray());
        return true;
    }
}

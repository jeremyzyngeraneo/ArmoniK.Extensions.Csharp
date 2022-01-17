﻿// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public class ZipArchiver
  {
    private static readonly string RootAppPath = "/tmp/packages";

    /// <summary>
    /// </summary>
    /// <param name="assemblyNameFilePath"></param>
    /// <returns></returns>
    public static bool IsZipFile(string assemblyNameFilePath)
    {
      //ATm ONLY Check the extensions 

      var extension = Path.GetExtension(assemblyNameFilePath);
      if (extension?.ToLower() == ".zip")
        return true;

      return false;
    }

    /// <summary>
    /// </summary>
    /// <param name="assemblyNameFilePath"></param>
    /// <returns></returns>
    /// <exception cref="WorkerApiException"></exception>
    public static IEnumerable<string> ExtractNameAndVersion(string assemblyNameFilePath)
    {
      string filePathNoExt;
      string appName;
      string versionName;

      try
      {
        filePathNoExt = Path.GetFileNameWithoutExtension(assemblyNameFilePath);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }

      // Instantiate the regular expression object.
      var pat = @"(.*)-v([\d\w]+\.[\d\w]+\.[\d\w]+)";

      var r = new Regex(pat,
                        RegexOptions.IgnoreCase);

      var m = r.Match(filePathNoExt);

      if (m.Success)
      {
        appName     = m.Groups[1].Value;
        versionName = m.Groups[2].Value;
      }
      else
      {
        throw new WorkerApiException("File name format doesn't match");
      }

      return new[] { appName, versionName };
    }

    public static string GetLocalPathToAssembly(string pathToZip)
    {
      string filePathNoExt;
      //Remove directory from path
      try
      {
        filePathNoExt = Path.GetFileNameWithoutExtension(pathToZip);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }

      var assemblyInfo    = ExtractNameAndVersion(pathToZip);
      var assemblyName    = assemblyInfo.ElementAt(0);
      var assemblyVersion = assemblyInfo.ElementAt(1);
      var basePath        = $"{RootAppPath}/{assemblyName}/{assemblyVersion}";

      return $"{basePath}/{assemblyName}.dll";
    }

    /// <summary>
    /// </summary>
    /// <param name="appVolume"></param>
    /// <param name="assemblyNameFilePath"></param>
    /// <param name="waitForArchiver"></param>
    /// <returns></returns>
    /// <exception cref="WorkerApiException"></exception>
    public static bool ArchiveAlreadyExtracted(string assemblyNameFilePath, int waitForArchiver = 300)
    {
      string filePathNoExt;
      //Remove directory from path
      try
      {
        filePathNoExt = Path.GetFileNameWithoutExtension(assemblyNameFilePath);
      }
      catch (ArgumentException e)
      {
        throw new WorkerApiException(e);
      }

      var assemblyInfo    = ExtractNameAndVersion(assemblyNameFilePath);
      var assemblyName    = assemblyInfo.ElementAt(0);
      var assemblyVersion = assemblyInfo.ElementAt(1);
      var basePath        = $"{RootAppPath}/{assemblyName}/{assemblyVersion}";

      if (Directory.Exists($"{RootAppPath}/{assemblyName}/{assemblyVersion}"))
      {
        //Now at least if dll exist or if a lock file exists and wait for unlock
        if (File.Exists($"{basePath}/{assemblyName}.dll"))
          return true;

        if (File.Exists($"{basePath}/{assemblyName}.lock"))
        {
          var retry       = 0;
          var loopingWait = 2; // 2 secs

          if (waitForArchiver == 0) return true;

          while (!File.Exists($"{basePath}/{assemblyName}.lock"))
          {
            Thread.Sleep(loopingWait * 1000);
            retry++;
            if (retry > waitForArchiver >> 2)
              throw new WorkerApiException($"Wait for unlock unzip was timeout after {waitForArchiver * 2} seconds");
          }
        }
      }

      return false;
    }

    /// <summary>
    ///   Unzip Archive if the temporary folder doesn't contain the
    ///   foler convention path should exist in /tmp/{AppName}/{AppVersion/AppName.dll
    /// </summary>
    /// <param name="assemblyNameFilePath">
    ///   The path to the zip file
    ///   Pattern for zip file has to be {AppName}-v{AppVersion}.zip
    /// </param>
    /// <returns>return string containing the path to the client assembly (.dll) </returns>
    public static string UnzipArchive(string assemblyNameFilePath)
    {
      if (!IsZipFile(assemblyNameFilePath))
        throw new WorkerApiException("Cannot yet extract or manage raw data other than zip archive");

      var assemblyInfo    = ExtractNameAndVersion(assemblyNameFilePath);
      var assemblyVersion = assemblyInfo.ElementAt(1);
      var assemblyName    = assemblyInfo.ElementAt(0);


      var pathToAssembly    = $"{RootAppPath}/{assemblyName}/{assemblyVersion}/{assemblyName}.dll";
      var pathToAssemblyDir = $"{RootAppPath}/{assemblyName}/{assemblyVersion}";

      if (ArchiveAlreadyExtracted(assemblyNameFilePath,
                                  0))
        return pathToAssembly;

      if (!Directory.Exists(pathToAssemblyDir))
        Directory.CreateDirectory(pathToAssemblyDir);

      var lockFileName = $"{pathToAssemblyDir}/{assemblyName}.lock";


      using (var fileStream = new FileStream(lockFileName,
                                             FileMode.OpenOrCreate,
                                             FileAccess.ReadWrite,
                                             FileShare.ReadWrite))
      {
        var lockfileForExtractionString = "Lockfile for extraction";

        var unicodeEncoding = new UnicodeEncoding();
        var textLength      = unicodeEncoding.GetByteCount(lockfileForExtractionString);

        if (fileStream.Length == 0)
          //Try to lock file to protect extraction
          fileStream.Write(new UnicodeEncoding().GetBytes(lockfileForExtractionString),
                           0,
                           unicodeEncoding.GetByteCount(lockfileForExtractionString));

        try
        {
          fileStream.Lock(0,
                          textLength);
        }
        catch (IOException)
        {
          return pathToAssembly;
        }
        catch (Exception e)
        {
          throw new WorkerApiException(e);
        }


        try
        {
          ZipFile.ExtractToDirectory(assemblyNameFilePath,
                                     RootAppPath);
        }
        catch (Exception e)
        {
          throw new WorkerApiException(e);
        }
        finally
        {
          fileStream.Unlock(0,
                            textLength);
        }
      }

      //Check now if the assembly is present
      if (!File.Exists(pathToAssembly))
        throw new WorkerApiException($"Fail to find assembly {pathToAssembly}. Something went wrong during the extraction. " +
                                     $"Please sure that tree folder inside is {assemblyName}/{assemblyVersion}/*.dll");

      return pathToAssembly;
    }
  }
}
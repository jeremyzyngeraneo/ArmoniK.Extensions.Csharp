// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023.All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

using ProtoBuf;

namespace ArmoniK.DevelopmentKit.Common;

[ProtoContract]
public class ArmonikPayload
{
  [ProtoMember(1)]
  public string MethodName { get; set; }

  [ProtoMember(2)]
  public byte[] ClientPayload { get; set; }

  [ProtoMember(3)]
  public bool SerializedArguments { get; set; }

  public byte[] Serialize()
  {
    if (ClientPayload is null)
    {
      throw new ArgumentNullException(nameof(ClientPayload));
    }

    var result = ProtoSerializer.SerializeMessageObject(this);

    return result;
  }

  public static ArmonikPayload Deserialize(byte[] payload)
  {
    if (payload == null || payload.Length == 0)
    {
      return new ArmonikPayload();
    }

    return ProtoSerializer.Deserialize<ArmonikPayload>(payload);
  }
}

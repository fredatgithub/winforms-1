﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.Serialization;

namespace System.Windows.Forms.BinaryFormat;

/// <summary>
///  Member type info.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/aa509b5a-620a-4592-a5d8-7e9613e0a03e">
///    [MS-NRBF] 2.3.1.2
///   </see>
///  </para>
/// </remarks>

internal readonly struct MemberTypeInfo : IBinaryWriteable, IEnumerable<(BinaryType Type, object? Info)>
{
    private readonly IList<(BinaryType Type, object? Info)> _info;

    public MemberTypeInfo(IList<(BinaryType Type, object? Info)> info) => _info = info;

    public readonly (BinaryType Type, object? Info) this[int index] => _info[index];
    public readonly int Count => _info.Count;

    public static MemberTypeInfo Parse(BinaryReader reader, Count expectedCount)
    {
        List<(BinaryType Type, object? Info)> info = new(expectedCount);

        // Get all of the BinaryTypes
        for (int i = 0; i < expectedCount; i++)
        {
            info.Add(((BinaryType)reader.ReadByte(), null));
        }

        // Check for more clarifying information

        for (int i = 0; i < expectedCount; i++)
        {
            BinaryType type = info[i].Type;
            switch (type)
            {
                case BinaryType.Primitive:
                case BinaryType.PrimitiveArray:
                    info[i] = (type, (PrimitiveType)reader.ReadByte());
                    break;
                case BinaryType.SystemClass:
                    info[i] = (type, reader.ReadString());
                    break;
                case BinaryType.Class:
                    info[i] = (type, ClassTypeInfo.Parse(reader));
                    break;
                case BinaryType.String:
                case BinaryType.ObjectArray:
                case BinaryType.StringArray:
                case BinaryType.Object:
                    // Other types have no additional data.
                    break;
                default:
                    throw new SerializationException("Unexpected binary type.");
            }
        }

        return new MemberTypeInfo(info);
    }

    public readonly void Write(BinaryWriter writer)
    {
        foreach ((BinaryType type, _) in this)
        {
            writer.Write((byte)type);
        }

        foreach ((BinaryType type, object? info) in this)
        {
            switch (type)
            {
                case BinaryType.Primitive:
                case BinaryType.PrimitiveArray:
                    writer.Write((byte)info!);
                    break;
                case BinaryType.SystemClass:
                    writer.Write((string)info!);
                    break;
                case BinaryType.Class:
                    ((ClassTypeInfo)info!).Write(writer);
                    break;
                case BinaryType.String:
                case BinaryType.ObjectArray:
                case BinaryType.StringArray:
                case BinaryType.Object:
                    // Other types have no additional data.
                    break;
                default:
                    throw new SerializationException("Unexpected binary type.");
            }
        }
    }

    IEnumerator<(BinaryType Type, object? Info)> IEnumerable<(BinaryType Type, object? Info)>.GetEnumerator()
        => _info.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _info.GetEnumerator();
}

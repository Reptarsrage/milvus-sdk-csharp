﻿using Google.Protobuf;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus vectors type.
/// </summary>
public enum MilvusVectorsType
{
    /// <summary>
    /// Float vectors.
    /// </summary>
    FloatVectors,

    /// <summary>
    /// Binary vectors.
    /// </summary>
    BinaryVectors,

    /// <summary>
    /// Ids.
    /// </summary>
    Ids,
}

/// <summary>
/// Vectors when calculating distance.
/// </summary>
public sealed class MilvusVectors
{
    /// <summary>
    /// Create BinaryVectors for calculating distance.
    /// </summary>
    /// <param name="binaryVectorFields"></param>
    /// <returns></returns>
    public static MilvusVectors CreateBinaryVectors(BinaryVectorField binaryVectorFields)
    {
        Verify.NotNull(binaryVectorFields);
        return new MilvusVectors(binaryVectorFields);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public static MilvusVectors CreateFloatVectors(IList<Field<float>> floatFields)
    {
        return new MilvusVectors(floatFields.Select(static p => p.Data.ToList()).ToList());
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <returns></returns>
    public static MilvusVectors CreateFloatVectors(IList<List<float>> floatFields)
    {
        return new MilvusVectors(floatFields);
    }

    /// <summary>
    /// Create float vector for calculating distance.
    /// </summary>
    /// <param name="floatFields"></param>
    /// <param name="dim">dimension of float fields.</param>
    /// <returns></returns>
    public static MilvusVectors CreateFloatVectors(IList<float> floatFields, int dim)
    {
        return new MilvusVectors(floatFields, dim);
    }

    /// <summary>
    /// Create ids for calculating distance.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public static MilvusVectors CreateIds(MilvusVectorIds ids)
    {
        Verify.NotNull(ids);

        return new MilvusVectors(ids);
    }

    /// <summary>
    /// Create a id array for calculating distance.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="ids">Ids</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <returns></returns>
    public static MilvusVectors CreateIds(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
    {
        return new MilvusVectors(ids: MilvusVectorIds.Create(collectionName, fieldName, ids, partitionNames));
    }

    internal Grpc.VectorsArray ToVectorsArray()
    {
        Grpc.VectorsArray vectorArray = new();

        if (MilvusVectorsType == MilvusVectorsType.FloatVectors)
        {

            vectorArray.DataArray = new Grpc.VectorField()
            {
                FloatVector = new Grpc.FloatArray(),
                Dim = Dim
            };
            vectorArray.DataArray.FloatVector.Data.AddRange(Vectors);
        }
        else if (MilvusVectorsType == MilvusVectorsType.BinaryVectors)
        {
            vectorArray.DataArray = new Grpc.VectorField()
            {
                BinaryVector = BinaryVectorField.ToGrpcFieldData().ToByteString(),
                Dim = Dim
            };
        }
        else if (MilvusVectorsType == MilvusVectorsType.Ids)
        {
            Grpc.VectorIDs grpcIds = Ids.ToGrpcIds();
            vectorArray.IdArray = grpcIds;
        }

        return vectorArray;
    }

    /// <summary>
    /// Ids
    /// </summary>
    [JsonPropertyName("ids")]
    public MilvusVectorIds Ids { get; }

    /// <summary>
    /// Vectors is an array of binary vector divided by given dim. Disabled when IDs is set.
    /// </summary>
    [JsonPropertyName("vectors")]
    public IList<float> Vectors { get; }

    /// <summary>
    /// Binary vector field.
    /// </summary>
    [JsonIgnore]
    [JsonPropertyName("binary_vectors")]
    public BinaryVectorField BinaryVectorField { get; }

    /// <summary>
    /// Milvus vectors type.
    /// </summary>
    [JsonIgnore]
    public MilvusVectorsType MilvusVectorsType { get; }

    /// <summary>
    /// Dim of vectors or binary_vectors, not needed when use ids
    /// </summary>
    [JsonPropertyName("dim")]
    public long Dim { get; set; }

    #region Private Methods ===================================================================
    private MilvusVectors(BinaryVectorField binaryVectorField)
    {
        MilvusVectorsType = MilvusVectorsType.BinaryVectors;
        BinaryVectorField = binaryVectorField;
        Dim = binaryVectorField.RowCount;
    }

    private MilvusVectors(MilvusVectorIds ids)
    {
        MilvusVectorsType = MilvusVectorsType.Ids;
        Ids = ids;
        Dim = ids.Dim;
    }

    private MilvusVectors(IList<List<float>> floatFields)
    {
        MilvusVectorsType = MilvusVectorsType.FloatVectors;
        Dim = floatFields.First().Count;
        Vectors = floatFields.SelectMany(static list => list).ToList();
    }

    private MilvusVectors(IList<float> floatFields, int dim)
    {
        MilvusVectorsType = MilvusVectorsType.FloatVectors;
        Dim = dim;
        Vectors = floatFields;
    }
    #endregion
}

/// <summary>
/// Vector when calculating distance.
/// </summary>
public sealed class MilvusVectorIds
{
    /// <summary>
    /// Collection name.
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; }

    /// <summary>
    /// Field name.
    /// </summary>
    [JsonPropertyName("field_name")]
    public string FieldName { get; }

    /// <summary>
    /// Ids.
    /// </summary>
    [JsonIgnore]
    public IList<long> IntIds { get; }

    /// <summary>
    /// String Ids.
    /// </summary>
    [JsonIgnore]
    public IList<string> StringIds { get; }

    /// <summary>
    /// Ids.
    /// </summary>
    [JsonPropertyName("id_array")]
    public object IdArray
    {
        get
        {
            if (IntIds != null) { return IntIds; }
            else { return StringIds; }
        }
    }

    /// <summary>
    /// Partition names.
    /// </summary>
    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; }

    /// <summary>
    /// Dimension of ids.
    /// </summary>
    [JsonIgnore]
    public long Dim => IntIds?.Count ?? StringIds?.Count ?? 0;

    /// <summary>
    /// Create a long MilvusVectorIds.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="ids">IDs</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <returns></returns>
    public static MilvusVectorIds Create(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
    {
        return new MilvusVectorIds(collectionName, fieldName, ids, partitionNames);
    }

    /// <summary>
    /// Create a string MilvusVectorIds.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="ids">IDs</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <returns></returns>
    public static MilvusVectorIds Create(
        string collectionName,
        string fieldName,
        IList<string> ids,
        IList<string> partitionNames = null)
    {
        return new MilvusVectorIds(collectionName, fieldName, ids, partitionNames);
    }

    internal Grpc.VectorIDs ToGrpcIds()
    {
        Grpc.VectorIDs idArray = new()
        {
            FieldName = FieldName,
            CollectionName = CollectionName,
        };

        Grpc.IDs ids = new();

        if (IntIds != null)
        {
            ids.IntId = new Grpc.LongArray();
            ids.IntId.Data.AddRange(IntIds);
        }
        else if (StringIds != null)
        {
            ids.StrId = new Grpc.StringArray();
            ids.StrId.Data.AddRange(StringIds);
        }

        idArray.IdArray = ids;

        return idArray;
    }

    #region Private =================================================================
    private MilvusVectorIds(
        string collectionName,
        string fieldName,
        IList<long> ids,
        IList<string> partitionNames = null)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        IntIds = ids;
        PartitionNames = partitionNames;
    }

    private MilvusVectorIds(
        string collectionName,
        string fieldName,
        IList<string> ids,
        IList<string> partitionNames = null)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        StringIds = ids;
        PartitionNames = partitionNames;
    }
    #endregion
}

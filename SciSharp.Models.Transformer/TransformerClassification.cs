﻿using System;
using System.Collections.Generic;
using System.Text;
using Tensorflow;
using Tensorflow.NumPy;
using Tensorflow.Common.Types;
using Tensorflow.Keras;
using Tensorflow.Keras.ArgsDefinition;
using Tensorflow.Keras.Engine;
using Tensorflow.Keras.Saving;
using static Tensorflow.KerasApi;

namespace SciSharp.Models.Transformer
{
    public class TransformerClassificationArgs : AutoSerializeLayerArgs
    {
        public int Maxlen { get; set; }
        public int VocabSize { get; set; }
        public int EmbedDim { get; set; }
        public int NumHeads { get; set; }
        public int FfDim { get; set; }
        public float DropoutRate { get; set; } = 0.1f;
        public int DenseDim { get; set; }
        public override IRegularizer ActivityRegularizer { get => base.ActivityRegularizer; set => base.ActivityRegularizer = value; }
    }
    public class TransformerClassification : Layer
    {
        TransformerClassificationArgs args;
        ILayer embedding_layer;
        ILayer transformer_block;
        ILayer pooling;
        ILayer dropout1;
        ILayer dense;
        ILayer dropout2;
        ILayer output;

        public TransformerClassification(TransformerClassificationArgs args)
             : base(new LayerArgs
             {
                 DType = args.DType,
                 Name = args.Name,
                 InputShape = args.InputShape,
                 BatchSize = args.BatchSize
             })
        {
            this.args = args;
        }
        public override void build(KerasShapesWrapper input_shape)
        {
            embedding_layer = new TokenAndPositionEmbedding(new TokenAndPositionEmbeddingArgs { Maxlen = args.Maxlen, VocabSize = args.VocabSize, EmbedDim = args.EmbedDim });
            transformer_block = new TransformerBlock(new TransformerBlockArgs { EmbedDim = args.EmbedDim, NumHeads = args.NumHeads, FfDim = args.FfDim });
            pooling = keras.layers.GlobalAveragePooling1D();
            dropout1 = keras.layers.Dropout(args.DropoutRate);
            dense = keras.layers.Dense(args.DenseDim, activation: "relu");
            dropout2 = keras.layers.Dropout(args.DropoutRate);
            output = keras.layers.Dense(2, activation: "softmax");
            StackLayers(embedding_layer, transformer_block, pooling, dropout1, dense, dropout2, output);
        }
        protected override Tensors Call(Tensors inputs, Tensors state = null, bool? training = null, IOptionalArgs? optional_args = null)
        {
            var embeddings = embedding_layer.Apply(inputs);
            var outputs = transformer_block.Apply(embeddings);
            outputs = pooling.Apply(outputs);
            outputs = dropout1.Apply(outputs);
            outputs = dense.Apply(outputs);
            outputs = dropout2.Apply(outputs);
            outputs = output.Apply(outputs);
            return outputs;
        }
        public static IModel Build(TransformerClassificationConfig cfg)
        {
            var inputs = keras.layers.Input(shape: new[] { cfg.DatasetCfg.maxlen });
            var transformer = new TransformerClassification(
                new TransformerClassificationArgs
                {
                    Maxlen = cfg.DatasetCfg.maxlen,
                    VocabSize = cfg.DatasetCfg.vocab_size,
                    EmbedDim = cfg.ModelCfg.embed_dim,
                    FfDim = cfg.ModelCfg.ff_dim,
                    DropoutRate = cfg.ModelCfg.dropout_rate,
                    DenseDim = cfg.ModelCfg.dense_dim
                });
            var outputs = transformer.Apply(inputs);
            return keras.Model(inputs: inputs, outputs: outputs);
        }

        public static ICallback Train(TransformerClassificationConfig? cfg)
        {
            cfg = cfg ?? new TransformerClassificationConfig();
            var dataloader = new IMDbDataset(cfg);
            var dataset = dataloader.GetData();
            var x_train = dataset[0];
            var y_train = dataset[1];
            var x_val = dataset[2];
            var y_val = dataset[3];
            var model = Build(cfg);
            model.summary();
            model.compile(optimizer: keras.optimizers.Adam(learning_rate: 0.01f), loss: keras.losses.SparseCategoricalCrossentropy(), metrics: new string[] { "accuracy" });
            var history = model.fit((NDArray)x_train, (NDArray)y_train, batch_size: cfg.TrainCfg.batch_size, epochs: cfg.TrainCfg.epochs, validation_data: ((NDArray val_x, NDArray val_y))(x_val, y_val));
            return history;
        }
    }
}

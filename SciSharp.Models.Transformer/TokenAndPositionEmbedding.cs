﻿using Serilog.Debugging;
using System;
using System.Collections.Generic;
using System.Text;
using Tensorflow;
using Tensorflow.Common.Types;
using Tensorflow.Keras;
using Tensorflow.Keras.ArgsDefinition;
using Tensorflow.Keras.Engine;
using Tensorflow.Keras.Saving;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;

namespace SciSharp.Models.Transformer
{
    public class TokenAndPositionEmbeddingArgs : Tensorflow.Keras.ArgsDefinition.AutoSerializeLayerArgs
    {
        public int Maxlen { get; set; }
        public int VocabSize { get; set; }
        public int EmbedDim { get; set; }
        public override IRegularizer ActivityRegularizer { get => base.ActivityRegularizer; set => base.ActivityRegularizer = value; }
    }
    public class TokenAndPositionEmbedding : Layer
    {
        TokenAndPositionEmbeddingArgs args;
        Tensor positions_base;
        ILayer token_emb;
        ILayer pos_emb;
        ILayer add;

        public TokenAndPositionEmbedding(TokenAndPositionEmbeddingArgs args)
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
            positions_base = tf.range(start: 0, limit: args.Maxlen, delta: 1);
            token_emb = keras.layers.Embedding(input_dim: args.VocabSize, output_dim: args.EmbedDim);
            pos_emb = keras.layers.Embedding(input_dim: args.Maxlen, output_dim: args.EmbedDim);
            add = keras.layers.Add();
            StackLayers(token_emb, pos_emb, add);
        }

        protected override Tensors Call(Tensors inputs, Tensors state = null, bool? training = null, IOptionalArgs? optional_args = null)
        {
            var embedding = token_emb.Apply(inputs);
            var positions = pos_emb.Apply(positions_base);
            var output = add.Apply(new Tensors(embedding, positions));
            return output;
        }
    }
}

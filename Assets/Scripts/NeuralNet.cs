using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System.IO;

public class NeuralNet : System.IComparable<NeuralNet>
{
    public Matrix<float> inputLayer;
    public Matrix<float> outputLayer;

    public List<Matrix<float>> deepLayers;
    public List<Matrix<float>> weights;
    public List<Matrix<float>> biases;

    public float fitness;

    private int inputCount;
    private int layerCount;
    private int neuronCount;
    private int outputCount;

    public NeuralNet(int nInputs, int nLayers, int nNeurons, int nOutputs)
    {
        inputCount = nInputs;
        layerCount = nLayers;
        neuronCount = nNeurons;
        outputCount = nOutputs;

        inputLayer = Matrix<float>.Build.Dense(1, nInputs);
        outputLayer = Matrix<float>.Build.Dense(1, nOutputs);

        deepLayers = new List<Matrix<float>>();
        weights = new List<Matrix<float>>();
        biases = new List<Matrix<float>>();

        // Input weights
        weights.Add(Matrix<float>.Build.Dense(nInputs, nNeurons));
        biases.Add(Matrix<float>.Build.Dense(1, nNeurons));

        // Hidden weights
        for (int i = 0; i < nLayers; i++)
        {
            deepLayers.Add(Matrix<float>.Build.Dense(1, nNeurons));

            if (i > 0)
            {
                weights.Add(Matrix<float>.Build.Dense(nNeurons, nNeurons));
                biases.Add(Matrix<float>.Build.Dense(1, nNeurons));
            }
        }

        // Output weights
        weights.Add(Matrix<float>.Build.Dense(nNeurons, nOutputs));
        biases.Add(Matrix<float>.Build.Dense(1, nOutputs));

        fitness = 0;
    }

    public void FillRandomly()
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].ColumnCount; j++)
                for (int k = 0; k < weights[i].RowCount; k++)
                    weights[i][k, j] = Random.Range(-1.0f, 1.0f);

            for (int j = 0; j < biases[i].ColumnCount; j++)
                biases[i][0, j] = Random.Range(-1.0f, 1.0f);
        }
    }

    public Matrix<float> Fire(float[] input)
    {
        for (int i = 0; i < inputLayer.ColumnCount; i++)
        {
            inputLayer[0, i] = input[i];
        }

        for (int i = 0; i < deepLayers.Count; i++)
        {
            if (i == 0)
                deepLayers[i] = ((inputLayer * weights[i]) + biases[i]).PointwiseTanh();
            else
                deepLayers[i] = ((deepLayers[i - 1] * weights[i]) + biases[i]).PointwiseTanh();
        }

        outputLayer = ((deepLayers[deepLayers.Count - 1] * weights[weights.Count - 1]) + biases[biases.Count - 1]).PointwiseTanh();

        return outputLayer;
    }

    public void Mutate(float rate, float strength)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].ColumnCount; j++)
                for (int k = 0; k < weights[i].RowCount; k++)
                    weights[i][k, j] = Random.Range(0f, 1.0f) < rate ?
                        Mathf.Clamp(weights[i][k, j] + Random.Range(-strength, strength), -1.0f, 1.0f)
                        : weights[i][k, j];

            for (int j = 0; j < biases[i].ColumnCount; j++)
                biases[i][0, j] = Random.Range(0f, 1.0f) < rate ?
                        Mathf.Clamp(biases[i][0, j] + Random.Range(-strength, strength), -1.0f, 1.0f)
                        : biases[i][0, j];
        }
    }

    public NeuralNet Mate(NeuralNet partner)
    {
        NeuralNet child = new NeuralNet(inputCount, layerCount, neuronCount, outputCount);

        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].ColumnCount; j++)
                for (int k = 0; k < weights[i].RowCount; k++)
                    child.weights[i][k, j] = Random.Range(0, 2) == 0 ? partner.weights[i][k, j] : weights[i][k, j];

            for (int j = 0; j < biases[i].ColumnCount; j++)
                child.biases[i][0, j] = Random.Range(0, 2) == 0 ? partner.biases[i][0, j] : biases[i][0, j];
        }

        return child;
    }

    public void CopyTo(NeuralNet nn)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].ColumnCount; j++)
                for (int k = 0; k < weights[i].RowCount; k++)
                    nn.weights[i][k, j] = weights[i][k, j];

            for (int j = 0; j < biases[i].ColumnCount; j++)
                nn.biases[i][0, j] = biases[i][0, j];
        }
    }

    public void SaveState(string fileName)
    {
        string path = "Assets/NetworkSaves/";

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (!File.Exists(path + fileName))
            File.Create(path + fileName).Close();

        StreamWriter sw = new StreamWriter(path + fileName, false);

        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].ColumnCount; j++)
                for (int k = 0; k < weights[i].RowCount; k++)
                    sw.WriteLine(weights[i][k, j].ToString());

            for (int j = 0; j < biases[i].ColumnCount; j++)
                sw.WriteLine(biases[i][0, j].ToString());
        }

        sw.Close();
    }

    public void LoadState(string fileName)
    {
        string path = "Assets/NetworkSaves/";

        if (File.Exists(path + fileName))
        {
            StreamReader sr = new StreamReader(path + fileName);

            for (int i = 0; i < weights.Count; i++)
            {
                for (int j = 0; j < weights[i].ColumnCount; j++)
                    for (int k = 0; k < weights[i].RowCount; k++)
                        weights[i][k, j] = float.Parse(sr.ReadLine());

                for (int j = 0; j < biases[i].ColumnCount; j++)
                    biases[i][0, j] = float.Parse(sr.ReadLine());
            }

            sr.Close();
        }
    }

    public int CompareTo(NeuralNet other)
    {
        if (other == null)
            return 1;
        else if (fitness > other.fitness)
            return 1;
        else if (fitness < other.fitness)
            return -1;
        else
            return 0;
    }
}

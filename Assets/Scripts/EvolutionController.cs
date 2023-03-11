using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EvolutionController : MonoBehaviour
{
    public GameObject prefab;
    public float TimeScale = 20;
    public int PopulationSize = 50;
    public int Layers = 1;
    public int Neurons = 10;
    public float MutationRate = 0.05f;
    public float MutationStrength = 0.1f;
    public int currentGeneration = 0;
    public bool PreTrained = false;
    public int ResumeGeneration = -1;
    public float highestFitness = 0;

    private List<NeuralNet> population;
    private List<CarController> cars;

    void Start()
    {
        population = new List<NeuralNet>();
        cars = new List<CarController>();

        if (ResumeGeneration != -1)
            currentGeneration = ResumeGeneration;

        if (PreTrained)
        {
            PopulationSize = 1;

            NeuralNet nn = new NeuralNet(5, Layers, Neurons, 2);
            nn.LoadState("Gen" + currentGeneration);
            population.Add(nn);

            CarController c = Instantiate(prefab).GetComponent<CarController>();
            c.brain = nn;
            cars.Add(c);
        }
        else
        {
            for (int i = 0; i < PopulationSize; i++)
            {
                NeuralNet nn = new NeuralNet(5, Layers, Neurons, 2);
                if (ResumeGeneration == -1)
                    nn.FillRandomly();
                else
                    nn.LoadState("Gen" + ResumeGeneration);
                population.Add(nn);

                CarController c = Instantiate(prefab).GetComponent<CarController>();
                c.brain = nn;
                cars.Add(c);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Time.timeScale = TimeScale;

        int countKilled = 0;
        for (int i = 0; i < PopulationSize; i++)
        {
            if (cars[i].killed)
                countKilled++;

            if (cars[i].brain.fitness > highestFitness)
            {
                highestFitness = cars[i].brain.fitness;
                //if (highestFitness > 200.0f)
                    //cars[i].brain.SaveState("Fitness" + cars[i].brain.fitness);
            }
        }

        if (countKilled == PopulationSize)
            NextGen();
    }

    private void NextGen()
    {
        currentGeneration++;

        if (!PreTrained)
        {
            List<NeuralNet> newPopulation = new List<NeuralNet>();

            population.Sort();

            List<int> ids = new List<int>();

            for (int i = 0; i < PopulationSize; i++)
            {
                for (int j = 0; j < i + 1; j++)
                    ids.Add(i);
            }

            for (int i = 0; i < PopulationSize; i++)
            {
                int id1 = ids[UnityEngine.Random.Range(0, ids.Count)];
                int id2;

                do
                {
                    id2 = ids[UnityEngine.Random.Range(0, ids.Count)];
                }
                while (id2 == id1);

                NeuralNet partner1 = population[id1];
                NeuralNet partner2 = population[id2];

                NeuralNet child = partner1.Mate(partner2);
                child.Mutate(MutationRate, MutationStrength);

                newPopulation.Add(child);
            }

            population = new List<NeuralNet>(newPopulation);
        }

        for (int i = 0; i < PopulationSize; i++)
        {
            if (PreTrained)
                cars[i].brain.LoadState("Gen" + currentGeneration);
            else
                cars[i].brain = population[i];

            cars[i].Reset();
        }
    }
}

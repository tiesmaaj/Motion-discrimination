using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockGenerator : MonoBehaviour
{
    // List possible trial conditions. Initially using 2-AFC left-or-right motion discrimination task.
    private List<TrialDef> trialDefs;
    public float[] coherences = new float[]{0.05f, .1f, .15f, .2f, .4f};
    public int[] directions = new int[]{1, -1};
    // public int[] y_directions = new int[]{1, -1};
    // public int[] z_directions = new int[]{1, -1};
    public int trialsPerCondition = 10;

    public List<TrialDef> GenerateBlock()
    {
        trialDefs = new List<TrialDef>();
        foreach (float coh in coherences)
        {
            foreach (int dir in directions)
            {
                for (int iTrial = 0; iTrial < trialsPerCondition; iTrial++)
                {
                    TrialDef newTrial = new TrialDef(coh, dir);
                    trialDefs.Add(newTrial);
                }
            }
        }

        trialDefs = ShuffleList(trialDefs);
        return trialDefs;
    }
    
    private List<T> ShuffleList<T>(List<T> list)
    {
        System.Random random = new System.Random();
        return list.OrderBy(x => random.Next()).ToList();
    }
}
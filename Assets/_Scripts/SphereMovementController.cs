using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO; // For file handling

public class SphereMovementController : MonoBehaviour
{
    public List<GameObject> spheres = new List<GameObject>(); // Reference to dynamically created spheres
    public Camera mainCamera; // Reference to the main camera
    public float speed = 2f; // Movement speed
    public float distanceFromCamera = 5f; // Distance from the camera
    public float visibleTime = 0.7f; // Visible time before the spheres disappear
    public float signal_circleRadius = 2f; // The radius of the circular space for random positions
    public float noise_circleRadius = 3f; // The radius of the circular space for random positions
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    public TextMeshProUGUI questionMarkText; // Reference to the UI Text element for the question mark
    public TextMeshProUGUI fixationText; // Declare the fixationText as public so it can be assigned in the editor

    private int currentLoop = 0;
    private int direction; // Direction of the signal dots (-1 for left, 1 for right)
    private float selectedCoherence; // Coherence for the current trial
    private List<float[]> trialData = new List<float[]>(); // Store trial direction, coherence, and input

    public int numSpheres = 50; // Number of spheres (total for noise and signal)
    public float dotScale = 0.25f; // Scale of each sphere (dot)

    private List<TrialDef> trialDefs;
    public BlockGenerator blockGen;

    void Start()
    {
        questionMarkText.gameObject.SetActive(false);
        fixationText.gameObject.SetActive(true); // Ensure the fixation cross is visible
        trialDefs = blockGen.GenerateBlock();

        // Create spheres at the start of the task
        CreateSpheres();
        StartCoroutine(StartMovementLoop());
    }

    void CreateSpheres()
    {
        GameObject RDK = new GameObject("RDK"); // Parent object for all spheres
        for (int i = 0; i < numSpheres; i++)
        {
            GameObject newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newSphere.transform.localScale = Vector3.one * dotScale; // Set scale of the sphere

            // Set the sphere's parent to the RDK object
            newSphere.transform.SetParent(RDK.transform);
            newSphere.SetActive(false); // Initially set spheres to inactive
            spheres.Add(newSphere); // Add the new sphere to the list
        }
    }

    IEnumerator StartMovementLoop()
    {
        while (currentLoop < trialDefs.Count)
        {
            // Logic for setting signal and noise dots
            selectedCoherence = trialDefs[currentLoop].vis_coherence;
            int signalCount = Mathf.RoundToInt(spheres.Count * selectedCoherence);
            int noiseCount = spheres.Count - signalCount;

            direction = trialDefs[currentLoop].vis_direction;

            List<GameObject> signalDots = new List<GameObject>();
            List<GameObject> noiseDots = new List<GameObject>();

            for (int i = 0; i < spheres.Count; i++)
            {
                if (i < signalCount)
                {
                    signalDots.Add(spheres[i]);
                }
                else
                {
                    noiseDots.Add(spheres[i]);
                }
            }

            // Randomize the position of signal spheres
            foreach (var dot in signalDots)
            {
                dot.transform.position = GetRandomPositionInCircle(signal_circleRadius);
                dot.SetActive(true); // Make the sphere visible
            }

            // Randomize the position of noise spheres
            foreach (var dot in noiseDots)
            {
                dot.transform.position = GetRandomPositionInCircle(noise_circleRadius);
                dot.SetActive(true); // Make the sphere visible
            }
            // Start moving signal and noise dots
            StartCoroutine(MoveSignalDots(signalDots, direction));
            StartCoroutine(MoveNoiseDots(noiseDots));

            yield return new WaitForSeconds(visibleTime); // Wait for movement to complete

            // Hide all spheres after the trial
            foreach (var dot in spheres)
            {
                dot.SetActive(false);
            }

            currentLoop++;
            questionMarkText.gameObject.SetActive(true);
            yield return StartCoroutine(WaitForKeyPress());
            questionMarkText.gameObject.SetActive(false);
        }

        // Delete the spheres at the end of the task
        DeleteSpheres();

        // Save trial data to CSV after the experiment
        SaveTrialDataToCSV();
    }

    // Method to delete all created spheres at the end of the task
    void DeleteSpheres()
    {
        foreach (GameObject sphere in spheres)
        {
            Destroy(sphere); // Destroy each sphere object
        }
        spheres.Clear(); // Clear the list of references
        Debug.Log("All spheres deleted");
    }

    // Move the signal dots in a straight line (left or right)
    IEnumerator MoveSignalDots(List<GameObject> signalDots, int direction)
    {
        foreach (var dot in signalDots)
        {
            dot.SetActive(true); // Make the signal dots visible
        }

        float elapsedTime = 0f;
        Vector3 moveDirection = new Vector3(direction, 0, 0); // Move left or right

        while (elapsedTime < visibleTime)
        {
            foreach (var dot in signalDots)
            {
                dot.transform.Translate(moveDirection * speed * Time.deltaTime);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Move the noise dots in random directions
    IEnumerator MoveNoiseDots(List<GameObject> noiseDots)
    {
        foreach (var dot in noiseDots)
        {
            dot.SetActive(true); // Make the noise dots visible
        }

        float elapsedTime = 0f;
        Dictionary<GameObject, Vector3> currentDirections = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, float> directionChangeTimers = new Dictionary<GameObject, float>(); // Stores individual change times

        // Assign random initial direction and random direction change interval for each dot
        foreach (var dot in noiseDots)
        {
            currentDirections[dot] = GetRandomDirection();
            directionChangeTimers[dot] = Random.Range(0.2f, 0.7f); // Randomly change direction between 200 ms and 700 ms
        }

        // Move the dots for the visible duration
        while (elapsedTime < visibleTime)
        {
            foreach (var dot in noiseDots)
            {
                dot.transform.Translate(currentDirections[dot] * speed * Time.deltaTime);

                // Check if it's time to change direction based on the dot's individual timer
                directionChangeTimers[dot] -= Time.deltaTime;
                if (directionChangeTimers[dot] <= 0)
                {
                    currentDirections[dot] = GetRandomDirection(); // Assign a new random direction
                    directionChangeTimers[dot] = Random.Range(0.2f, 0.7f); // Reset the change timer to a new random value
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Generate a random position within a circle
    Vector3 GetRandomPositionInCircle(float circleRadius)
    {
        Vector2 randomPoint = Random.insideUnitCircle * circleRadius; // Get random point inside a circle of radius
        return mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera
            + new Vector3(randomPoint.x, randomPoint.y, 0); // Convert the 2D point to 3D space (x, y, 0)
    }

    // Get a random direction vector for the noise dots
    Vector3 GetRandomDirection()
    {
        return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    IEnumerator WaitForKeyPress()
    {
        bool responseGiven = false;
        int playerDirection = 0;

        while (!responseGiven)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                playerDirection = -1; // Player pressed left
                responseGiven = true;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                playerDirection = 1; // Player pressed right
                responseGiven = true;
            }

            yield return null;
        }

        // Store trial data: [direction, selectedCoherence, playerDirection]
        trialData.Add(new float[] { direction, selectedCoherence, playerDirection });
        CheckResponse(playerDirection);
    }

    void CheckResponse(int playerDirection)
    {
        if (playerDirection == direction)
        {
            Debug.Log("Correct!");
            audioSource.PlayOneShot(correctSound);
        }
        else
        {
            Debug.Log("Incorrect!");
            audioSource.PlayOneShot(incorrectSound);
        }
    }

    // Save the trial data to a CSV file
    void SaveTrialDataToCSV()
    {
        string filePath = Application.dataPath + "/TrialData.csv";
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("TrialDirection,Coherence,UserInput"); // Write headers

            foreach (var trial in trialData)
            {
                // Write each trial's data to the CSV file
                writer.WriteLine($"{trial[0]},{trial[1]},{trial[2]}");
            }
        }

        Debug.Log($"Trial data saved to {filePath}");
    }
}

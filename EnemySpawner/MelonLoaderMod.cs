using MelonLoader;
using StressLevelZero.AI;
using StressLevelZero.Pool;
using UnityEngine;
using System;
using System.Collections.Generic;
using EasyMenu;
using UnhollowerBaseLib;
using Valve.VRRenderingPackage;
using System.Linq;
using TMPro;
using System.Diagnostics;
using StressLevelZero.Interaction;
using UnityEngine.Events;
using PuppetMasta;
using StressLevelZero.UI;
using UnityEngine.UI;
using System.Runtime.Remoting;
using StressLevelZero.Data;
using Il2CppSystem.Linq.Expressions;

namespace EnemySpawner
{
    public static class BuildInfo
    {
        public const string Name = "EnemySpawner"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "Manch"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class Spawner
    {
        public int spawnerNumber;
        public int spawnPointID;
        public float lastUIUpdateTime = 0f;//needs to be reset

        //spawning
        public Vector3 position;
        public Vector3 customWalkPoint;
        public Quaternion rotation;

        //movement bools
        public string movementMode = "Agro to Player";

        //public bool useSpawnArea = false;
        public Vector2 spawnAreaSize = new Vector2(0f,0f);
        public float maxHeight = 10f;

        public bool UIEnabled = true;

        public bool useDistanceActivation = false;
        public bool hasPlayerLeft = false;//needs to be reset on creation
        public float distanceToActivate = 2f;
        public bool distanceCausedActivation = false;

        public bool spawningActive = false;//needs to be reset on creation

        public float spawnFrequency = 5f;
        public float cleardeadFrequency = 30f;
        public float lastSpawntime;//needs to be reset on creation
        public float lastClearDeadTime;//needs to be reset
        public int maxenemyCount = 8;

        public bool useSpawnLimit = false;
        public int spawnLimit = 20;
        public int totalSpawned;//needs to be reset on creation

        public bool useSpawnTimeLimit = false;
        public float spawnTimeLimit = 1f;
        public float spawnTime;//needs to be reset on creation
        public bool spawnTimeSet = false;//needs to be reset

        public bool spawnAreaVisualEnabled = false;
        public bool distanceAreaVisualEnabled = false;
        public bool heightVisualEnabled = false;

        //enemies
        public List<AIBrain> spawnedEnemies = new List<AIBrain>();
        public List<AIBrain> spawnedEnemiesIncludingDead = new List<AIBrain>();
        public List<int> enabledEnemies = new List<int>();
        public bool randomEnemy = true;

        //custom enemies vars
        public AISettings[] allAISettings = new AISettings[12];
        
    }

    public class AISettings
    {
        public string AI;
        public float aiHealth;
        public float roamSpeed;
        public float roamAngularSpeed;
        public float roamRangeX;
        public float roamRangeY;
        public float agroedSpeed;
        public float agroedAngularSpeed;
        public float investigationRange;
        public float breakAgroTargetDistance;
        public float breakAgroHomeDistance;
        public float visionFOV;
        public float hearingSensitivity;
        public float additionalMass;
        public Color defaultcrabletBaseColor;
        public string crabletBaseColor = "Default";
        public Color defaultcrabletAgroedColor;
        public string crabletAgroedColor = "Default";
        public float aggression;
        public float irritability;
        public float placeability;
        public float vengefulness;
        public float stunRecoveryTime;
        public float maxStunTime;
        public float minHeadImpact;
        public float minSpineImpact;
        public float minLimbImpact;
        public float restingRange;
        public bool freezeWhileResting;
        public bool homeIsPost;
        public float activeRange;
        public float roamFrequency;
        public bool roamWander;
        public bool enableThrowAttack;
        public float throwAttackMaxRange;
        public float throwAttackMinRange;
        public float throwCooldown;
        public float throwVelocity;
        public float gunRange;
        public float gunCooldown;
        public float gunAccuracy;
        public float reloadTime;
        public int clipSize;
        public int burstSize;
        public float desiredGunDistance;
    }

    public class EnemySpawner : MelonMod
    {
        static List<Spawner> activeSpawners = new List<Spawner>();
        static List<GameObject> physicalActiveSpawners = new List<GameObject>();
        List<string> allAI = new List<string>();
        static Pool[] allPools;
        Spawner selectedSpawner;

        GameObject spawnAreaVisual;
        GameObject distanceVisual;
        GameObject heightVisual;
        GameObject ghost;
        GameObject player;
        GameObject placeSpawnerText;
        static TMP_FontAsset font;
        Hand leftHand;
        Hand rightHand;
        bool createdGhost = false;
        bool foundPlayer = false;
        bool gotFont = false;
        bool placeSpawnerMode = false;
        bool setWalkPointMode = false;
        //bool appliedFont = false;
        int currentLevel;
        static int physicalSpawnPointIDTracker = 0;

        public override void OnApplicationStart()
        {
            CreateUI();
        }
        
        AISettings[] ConstructDefaultAISettings()
        {
            AISettings[] aISettings = new AISettings[12];
            //null body
            AISettings nullBody = new AISettings
            {
                AI = "Null Body",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 11f,
                breakAgroTargetDistance = 35f,
                breakAgroHomeDistance = 50f,
                visionFOV = 85f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0,0,0,0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 1f,
                irritability = 1f,
                placeability = 0f,
                vengefulness = 10f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 3f,
                minHeadImpact = 30f,
                minSpineImpact = 80f,
                minLimbImpact = 80f,
                restingRange = 4f,
                freezeWhileResting = true,
                homeIsPost = false,
                activeRange = 6,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[0] = nullBody;

            AISettings fordEarlyExit = new AISettings
            {
                AI = "Ford Early Exit",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.68f,
                agroedAngularSpeed = 180f,
                investigationRange = 11f,
                breakAgroTargetDistance = 35f,
                breakAgroHomeDistance = 50f,
                visionFOV = 90f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 1f,
                irritability = 1f,
                placeability = 0f,
                vengefulness = 10f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 3f,
                minHeadImpact = 40f,
                minSpineImpact = 80f,
                minLimbImpact = 80f,
                restingRange = 5f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 7,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[1] = fordEarlyExit;

            AISettings omniturret = new AISettings()
            {
                AI = "Omniturret",
                aiHealth = 100f,
                roamSpeed = 2.5f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 9f,
                breakAgroTargetDistance = 10f,
                breakAgroHomeDistance = 30f,
                visionFOV = 80f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 1f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 1f,
                maxStunTime = 2f,
                minHeadImpact = 2000f,
                minSpineImpact = 2000f,
                minLimbImpact = 2000f,
                restingRange = 3f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 5f,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 3f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[2] = omniturret;

            AISettings nullBodyCorrupted = new AISettings()
            {
                AI = "Null Body Corrupted",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 11f,
                breakAgroTargetDistance = 35f,
                breakAgroHomeDistance = 50f,
                visionFOV = 85f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 1f,
                irritability = 1f,
                placeability = 0f,
                vengefulness = 10f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 3f,
                minHeadImpact = 30f,
                minSpineImpact = 80f,
                minLimbImpact = 80f,
                restingRange = 4f,
                freezeWhileResting = true,
                homeIsPost = false,
                activeRange = 6,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[3] = nullBodyCorrupted;

            AISettings fordEarlyExitHeadset = new AISettings()
            {
                AI = "Ford Early Exit Headset",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.68f,
                agroedAngularSpeed = 180f,
                investigationRange = 11f,
                breakAgroTargetDistance = 35f,
                breakAgroHomeDistance = 50f,
                visionFOV = 90f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 1f,
                irritability = 1f,
                placeability = 0f,
                vengefulness = 10f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 3f,
                minHeadImpact = 40f,
                minSpineImpact = 80f,
                minLimbImpact = 80f,
                restingRange = 5f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 7,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[4] = fordEarlyExitHeadset;

            AISettings fordVRJunkie = new AISettings()
            {
                AI = "Ford VR Junkie",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 11f,
                breakAgroTargetDistance = 35f,
                breakAgroHomeDistance = 50f,
                visionFOV = 90f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 0.499f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 3f,
                minHeadImpact = 40f,
                minSpineImpact = 80f,
                minLimbImpact = 80f,
                restingRange = 5f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 7,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[5] = fordVRJunkie;

            AISettings ford = new AISettings()
            {
                AI = "Ford",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 11f,
                breakAgroTargetDistance = 35f,
                breakAgroHomeDistance = 50f,
                visionFOV = 90f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 0.499f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 3f,
                minHeadImpact = 40f,
                minSpineImpact = 80f,
                minLimbImpact = 80f,
                restingRange = 5f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 7,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[6] = ford;

            AISettings omniWrecker = new AISettings()
            {
                AI = "Omni Wrecker",
                aiHealth = 100f,
                roamSpeed = 2.5f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 9f,
                breakAgroTargetDistance = 25f,
                breakAgroHomeDistance = 40f,
                visionFOV = 80f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 1f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 2f,
                maxStunTime = 2f,
                minHeadImpact = 2000f,
                minSpineImpact = 2000f,
                minLimbImpact = 2000f,
                restingRange = 3f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 5,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[7] = omniWrecker;

            AISettings omniProjector = new AISettings()
            {
                AI = "Omni Projector",
                aiHealth = 100f,
                roamSpeed = 2.5f,
                roamAngularSpeed = 180f,
                roamRangeX = 8f,
                roamRangeY = 8f,
                agroedSpeed = 3f,
                agroedAngularSpeed = 180f,
                investigationRange = 14f,
                breakAgroTargetDistance = 40f,
                breakAgroHomeDistance = 60f,
                visionFOV = 90f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(7, 7, 7, 1),
                defaultcrabletAgroedColor = new Color(7, 7, 7, 1),
                aggression = 1f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 2f,
                maxStunTime = 1f,
                minHeadImpact = 100f,
                minSpineImpact = 2000f,
                minLimbImpact = 2000f,
                restingRange = 5f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 8,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 2f,
                reloadTime = 2.2f,
                clipSize = 15,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[8] = omniProjector;

            AISettings crablet = new AISettings()
            {
                AI = "Crablet",
                aiHealth = 100f,
                roamSpeed = 1.35f,
                roamAngularSpeed = 240f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 0.8f,
                agroedAngularSpeed = 240f,
                investigationRange = 12f,
                breakAgroTargetDistance = 60f,
                breakAgroHomeDistance = 70f,
                visionFOV = 78f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0.146f, 2.049f, 2.540f, 1.000f),
                defaultcrabletAgroedColor = new Color(2.540f, 0.310f, 0.143f, 1.000f),
                aggression = 0f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 2f,
                minHeadImpact = 8f,
                minSpineImpact = 8f,
                minLimbImpact = 10f,
                restingRange = 6f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 8,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[9] = crablet;

            AISettings crabletPlus = new AISettings()
            {
                AI = "Crablet Plus",
                aiHealth = 100f,
                roamSpeed = 3f,
                roamAngularSpeed = 180f,
                roamRangeX = 15f,
                roamRangeY = 15f,
                agroedSpeed = 4f,
                agroedAngularSpeed = 180f,
                investigationRange = 12f,
                breakAgroTargetDistance = 60f,
                breakAgroHomeDistance = 70f,
                visionFOV = 78f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0.146f, 2.049f, 2.540f, 1.000f),
                defaultcrabletAgroedColor = new Color(2.540f, 0.310f, 0.143f, 1.000f),
                aggression = 0f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 0.2f,
                maxStunTime = 2f,
                minHeadImpact = 64f,
                minSpineImpact = 64f,
                minLimbImpact = 56f,
                restingRange = 6f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 8,
                roamFrequency = 0.2f,
                roamWander = false,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[10] = crabletPlus;

            AISettings nullRat = new AISettings()
            {
                AI = "Null Rat",
                aiHealth = 100f,
                roamSpeed = 2.5f,
                roamAngularSpeed = 180f,
                roamRangeX = 1.8f,
                roamRangeY = 1.8f,
                agroedSpeed = 1.25f,
                agroedAngularSpeed = 180f,
                investigationRange = 9f,
                breakAgroTargetDistance = 10f,
                breakAgroHomeDistance = 30f,
                visionFOV = 80f,
                hearingSensitivity = 0f,
                additionalMass = 0f,
                defaultcrabletBaseColor = new Color(0, 0, 0, 0),
                defaultcrabletAgroedColor = new Color(0, 0, 0, 0),
                aggression = 0f,
                irritability = 1f,
                placeability = 1f,
                vengefulness = 1f,
                stunRecoveryTime = 1f,
                maxStunTime = 2f,
                minHeadImpact = 10f,
                minSpineImpact = 10f,
                minLimbImpact = 10f,
                restingRange = 3f,
                freezeWhileResting = false,
                homeIsPost = false,
                activeRange = 5,
                roamFrequency = 0.65f,
                roamWander = true,
                enableThrowAttack = false,
                throwAttackMaxRange = 20f,
                throwAttackMinRange = 3f,
                throwCooldown = 3f,
                throwVelocity = 4f,
                gunRange = 60f,
                gunCooldown = 1.2f,
                gunAccuracy = 3f,
                reloadTime = 2.2f,
                clipSize = 1,
                burstSize = 3,
                desiredGunDistance = 4f
            };
            aISettings[11] = nullRat;
            return aISettings;
        }

        public override void OnLevelWasLoaded(int level)
        {
            //Resetting varibles and clearing lists
            currentLevel = level;
            createdGhost = false;
            foundPlayer = false;
            selectedSpawner = null;
            activeSpawners.Clear();
            physicalActiveSpawners.Clear();

        }

        public static List<Spawner> GetActiveSpawners()
        {
            return(activeSpawners);
        }

        public static void LoadSpawner(Spawner loadedSpawner)
        {
            allPools = UnityEngine.Object.FindObjectsOfType<Pool>();
            if(allPools.Length < 180)
            {
                MelonModLogger.Log("You need to spawn a utility gun");
            }

            //building visuals for spawn point
            GameObject spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawnPoint.GetComponent<Renderer>().material.color = Color.black;
            spawnPoint.GetComponent<Collider>().enabled = false;
            spawnPoint.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            spawnPoint.transform.position = loadedSpawner.position;
            spawnPoint.transform.rotation = loadedSpawner.rotation;

            spawnPoint.name = (physicalSpawnPointIDTracker).ToString();

            //creating new spawnpoint class with settings
            Spawner newSpawner = new Spawner();
            newSpawner.spawnerNumber = activeSpawners.Count + 1;
            newSpawner.spawnPointID = physicalSpawnPointIDTracker;
            physicalSpawnPointIDTracker++;
            newSpawner.lastUIUpdateTime = 0f;
            newSpawner.position = loadedSpawner.position;
            newSpawner.customWalkPoint = loadedSpawner.customWalkPoint;
            newSpawner.movementMode = loadedSpawner.movementMode;
            newSpawner.rotation = loadedSpawner.rotation;
            newSpawner.spawnAreaSize = loadedSpawner.spawnAreaSize;
            newSpawner.UIEnabled = loadedSpawner.UIEnabled;
            if (!newSpawner.UIEnabled)
            {
                spawnPoint.SetActive(false);
            }
            newSpawner.useDistanceActivation = loadedSpawner.useDistanceActivation;
            newSpawner.distanceToActivate = loadedSpawner.distanceToActivate;
            
            newSpawner.spawningActive = true;//MAKING THIS OPTIONAL
            if (loadedSpawner.useDistanceActivation)
            {
                newSpawner.spawningActive = false;
            }

            //spawner disable after time settings
            newSpawner.useSpawnTimeLimit = loadedSpawner.useSpawnTimeLimit;
            newSpawner.spawnTimeLimit = loadedSpawner.spawnTimeLimit;

            //spawner disable after spawns settings
            newSpawner.useSpawnLimit = loadedSpawner.useSpawnLimit;
            newSpawner.spawnLimit = loadedSpawner.spawnLimit;

            newSpawner.maxenemyCount = loadedSpawner.maxenemyCount;
            newSpawner.spawnFrequency = loadedSpawner.spawnFrequency;
            newSpawner.cleardeadFrequency = loadedSpawner.cleardeadFrequency;
            newSpawner.lastSpawntime = 0f;

            newSpawner.useSpawnLimit = loadedSpawner.useSpawnLimit;
            newSpawner.spawnLimit = loadedSpawner.spawnLimit;

            //giving spawner default AI Values
            newSpawner.allAISettings = loadedSpawner.allAISettings;

            //Setting AI spawning values
            newSpawner.randomEnemy = loadedSpawner.randomEnemy;
            newSpawner.enabledEnemies = loadedSpawner.enabledEnemies;
            


            GameObject spawnPointName = new GameObject("SpawnPointName");
            CreateSpawnPointUI(spawnPointName, spawnPoint, new Vector3(0f, 15f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Bold, TextAlignmentOptions.Center, "Spawner " + (activeSpawners.Count + 1).ToString());
            
            GameObject spawnPointStatus = new GameObject("SpawnPointStatus");
            string status;
            if (newSpawner.spawningActive)
            {
                status = "Enabled";
            }
            else
            {
                status = "Disabled";
            }
            CreateSpawnPointUI(spawnPointStatus, spawnPoint, new Vector3(0f, 13f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, "Status: " + status);
            GameObject totalEnemiesSpawned = new GameObject("TotalEnemies");
            CreateSpawnPointUI(totalEnemiesSpawned, spawnPoint, new Vector3(0f, 11f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, "Total Spawned: 0");
            GameObject disableTimeStatus = new GameObject("disableTimeStatus");
            if (newSpawner.useSpawnTimeLimit)
            {
                status = "Time Left: " + newSpawner.spawnTimeLimit.ToString();
            }
            else
            {
                status = ("Time Left: Infinite");
            }
            CreateSpawnPointUI(disableTimeStatus, spawnPoint, new Vector3(0f, 9f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, status);
            GameObject disableSpawnStatus = new GameObject("spawnLimitStatus");
            if (newSpawner.useSpawnLimit)
            {
                status = "Spawns Left: " + newSpawner.spawnLimit.ToString();
            }
            else
            {
                status = "Spawns Left: Infinite";
            }
            CreateSpawnPointUI(disableSpawnStatus, spawnPoint, new Vector3(0f, 7f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, status);

            physicalActiveSpawners.Add(spawnPoint);
            activeSpawners.Add(newSpawner);
        }

        public override void OnUpdate()
        {
            /*
            if (Input.GetKeyDown(KeyCode.Keypad7))
            {
                GameObject[] gameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                //List<AmmoDispenser> aimEnhancers = new List<AmmoDispenser>();
                StressLevelZero.Rig.ControllerRig[] controllerRigs = new StressLevelZero.Rig.ControllerRig[1];

                foreach (GameObject obj in gameObjects)
                {
                    StressLevelZero.Rig.ControllerRig[] childAimEnhancers = obj.GetComponentsInChildren<StressLevelZero.Rig.ControllerRig>(true);
                    
                    if (childAimEnhancers.Length > 0)
                    {
                        controllerRigs = childAimEnhancers;
                        break;
                    }
                }

                if(controllerRigs[0] != null)
                {
                    controllerRigs[0].crouchEnabled = !controllerRigs[0].crouchEnabled;
                    controllerRigs[0].turnEnabled = !controllerRigs[0].turnEnabled;
                }
                else
                {
                    //MelonModLogger.Log("Could not find player data thing");
                }

            }
            */
            
            if (activeSpawners.Count > 0)
            {
                RunSpawnPoints();
            }

            /*
            //Ghost Spawner
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                ghostSpawner = !ghostSpawner;
                if (ghostSpawner)
                {
                    ghost.SetActive(true);
                }
                else
                {
                    ghost.SetActive(false);
                }
            }
            */
            /*
            if (Input.GetKeyDown(KeyCode.Keypad6))
            {
                AIBrain[] brains = UnityEngine.Object.FindObjectsOfType<AIBrain>();
                AIBrain spawnedAIBrain = brains[0];
                MelonModLogger.Log("AI is " + spawnedAIBrain.gameObject.name);
                MelonModLogger.Log("Health is " + spawnedAIBrain.behaviour.health.cur_hp);
                MelonModLogger.Log("roam speed is " + spawnedAIBrain.behaviour.roamSpeed);
                MelonModLogger.Log("roam angular speed is " + spawnedAIBrain.behaviour.roamAngSpeed);
                MelonModLogger.Log("roam range is Vector 2 with x: " + spawnedAIBrain.behaviour.roamRange.x + " and y: " + spawnedAIBrain.behaviour.roamRange.y);
                MelonModLogger.Log("agroed speed is " + spawnedAIBrain.behaviour.agroedSpeed);
                MelonModLogger.Log("agroed angular speed is " + spawnedAIBrain.behaviour.agroedAngSpeed);
                MelonModLogger.Log("Investigate range is " + spawnedAIBrain.behaviour.investigateRange);
                MelonModLogger.Log("break agro target distance is " + spawnedAIBrain.behaviour.breakAgroTargetDistance);
                MelonModLogger.Log("break agro home distance is " + spawnedAIBrain.behaviour.breakAgroHomeDistance);
                MelonModLogger.Log("Vision FOV is " + spawnedAIBrain.behaviour.sensors.visionFov);
                MelonModLogger.Log("hearing sensitivity is " + spawnedAIBrain.behaviour.sensors.hearingSensitivity);
                MelonModLogger.Log("Additional mass is " + spawnedAIBrain.behaviour.sensors.additionalMass);
                MelonModLogger.Log("crablet base color is " + spawnedAIBrain.behaviour.baseColor.ToString());
                MelonModLogger.Log("crablet agro color is " + spawnedAIBrain.behaviour.agroColor.ToString());
                MelonModLogger.Log("aggression is " + spawnedAIBrain.behaviour.health.aggression);
                MelonModLogger.Log("irritability is " + spawnedAIBrain.behaviour.health.irritability);
                MelonModLogger.Log("placeability is " + spawnedAIBrain.behaviour.health.placability);
                MelonModLogger.Log("vengefulness is " + spawnedAIBrain.behaviour.health.vengefulness);
                MelonModLogger.Log("stun recovery is " + spawnedAIBrain.behaviour.health.stunRecovery);
                MelonModLogger.Log("max stun seconds is " + spawnedAIBrain.behaviour.health.maxStunSeconds);
                MelonModLogger.Log("min head impact is " + spawnedAIBrain.behaviour.health.minHeadImpact);
                MelonModLogger.Log("min spine impact is " + spawnedAIBrain.behaviour.health.minSpineImpact);
                MelonModLogger.Log("min limb impact is " + spawnedAIBrain.behaviour.health.minLimbImpact);
                MelonModLogger.Log("Resting range is " + spawnedAIBrain.behaviour.restingRange);
                MelonModLogger.Log("freeze while resting is " + spawnedAIBrain.behaviour.freezeWhileResting.ToString());
                MelonModLogger.Log("Home is post is " + spawnedAIBrain.behaviour.homeIsPost.ToString());
                MelonModLogger.Log("Active range is " + spawnedAIBrain.behaviour.activeRange);
                MelonModLogger.Log("Roam frequency is " + spawnedAIBrain.behaviour.roamFrequency);
                MelonModLogger.Log("Roam wanders is " + spawnedAIBrain.behaviour.roamWanders.ToString());
                MelonModLogger.Log("enable throw attack is " + spawnedAIBrain.behaviour.enableThrowAttack.ToString());
                MelonModLogger.Log("Throw max range is " + spawnedAIBrain.behaviour.throwMaxRange);
                MelonModLogger.Log("Throw min range is " + spawnedAIBrain.behaviour.throwMinRange);
                MelonModLogger.Log("Throw cooldown is " + spawnedAIBrain.behaviour.throwCooldown);
                MelonModLogger.Log("Throw velocity is " + spawnedAIBrain.behaviour.throwVelocity);
                MelonModLogger.Log("Gun range is " + spawnedAIBrain.behaviour.gunRange);
                MelonModLogger.Log("gun cooldown is " + spawnedAIBrain.behaviour.gunCooldown);
                MelonModLogger.Log("accuracy is " + spawnedAIBrain.behaviour.accuracy);
                MelonModLogger.Log("reload time is " + spawnedAIBrain.behaviour.reloadTime);
                MelonModLogger.Log("clip size is " + spawnedAIBrain.behaviour.clipSize);
                MelonModLogger.Log("burst size is " + spawnedAIBrain.behaviour.burstSize);
                MelonModLogger.Log("desired gun distance is " + spawnedAIBrain.behaviour.desiredGunDistance);

                //agro on NPC type is here

                
                MelonModLogger.Log("is sound ago when is secondary zone is " + spawnedAIBrain.behaviour.isSoundAggroWhenInSecondaryZone.ToString());
                //mental state
                //loco state
                MelonModLogger.Log("ai tick frequency is " + spawnedAIBrain.behaviour.aiTickFreq);
                MelonModLogger.Log("Homebound bool is " + spawnedAIBrain.behaviour._homeBound.ToString());
                MelonModLogger.Log("Investigate is position bool is " + spawnedAIBrain.behaviour.investIsPosition.ToString());
                //Vector3's for target positioning
                MelonModLogger.Log("investigation cooldown is " + spawnedAIBrain.behaviour._investigationCooldown);
                MelonModLogger.Log("block collisions until is " + spawnedAIBrain.behaviour._blockCollisionsUntil);
                MelonModLogger.Log("last ai tick time is " + spawnedAIBrain.behaviour._lastAiTickTime);
                MelonModLogger.Log("last jump time is " + spawnedAIBrain.behaviour._lastJumpTime);
                MelonModLogger.Log("cool down loco switch is " + spawnedAIBrain.behaviour._cooldownLocoSwitch);
                MelonModLogger.Log("melee attack active is " + spawnedAIBrain.behaviour.meleeAttackActive.ToString());
                //target distance?

                //SENSORS
                
                
                
                MelonModLogger.Log("foot supported is " + spawnedAIBrain.behaviour.sensors.footSupported);
                MelonModLogger.Log("hand supported is " + spawnedAIBrain.behaviour.sensors.handSupported);
                MelonModLogger.Log("body supported is " + spawnedAIBrain.behaviour.sensors.bodySupported);
                MelonModLogger.Log("total supported is " + spawnedAIBrain.behaviour.sensors.totalSupported);
                MelonModLogger.Log("total mass is " + spawnedAIBrain.behaviour.sensors.TotalMass);

                //SOUND
                MelonModLogger.Log("Pitch multiplier is " + spawnedAIBrain.behaviour.sfx.pitchMultiplier);

                //HEALTH
                MelonModLogger.Log("max hit points is " + spawnedAIBrain.behaviour.health.maxHitPoints);
                MelonModLogger.Log("max appendage hp is " + spawnedAIBrain.behaviour.health.maxAppendageHp);
                
               
                
                //MelonModLogger.Log("current mental state is " + Enum.GetName(typeof(PuppetMasta.BehaviourBaseNav.MentalState), spawnedAIBrain.behaviour.mentalState));
            }
            */

            //Grabbing/Creating things
            if (!foundPlayer)
            {
                if(GameObject.FindGameObjectWithTag("Player") != null)
                {
                    player = GameObject.FindGameObjectWithTag("Player");
                    foundPlayer = true;
                }
            }

            if (leftHand == null || rightHand == null)
            {
                GetHands();
            }

            if (!createdGhost)
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ghost.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                ghost.GetComponent<Renderer>().material.color = Color.green;
                ghost.GetComponent<Collider>().enabled = false;
                createdGhost = true;
                ghost.SetActive(false);
            }

            /*
            if (currentLevel == 1 && !appliedFont)
            {
                if(font!=null && desiredGunDistanceElement.GetTextObject() != null)
                {
                    ApplyUIFont();
                }
            }
            */
            if (currentLevel == 1 && !gotFont)
            {
                GetFont();
            }

            if (placeSpawnerMode)
            {
                ghost.transform.position = GetGroundPointFromGameObject(player);
                if (leftHand.controller.GetPrimaryInteractionButtonDown())
                {
                    if (CreateSpawnPoint())
                    {
                        GameObject.Destroy(placeSpawnerText);
                        ghost.SetActive(false);

                        GameObject text = configureSpawnerElement.GetTextObject();
                        TextMeshPro tmp = text.GetComponent<TextMeshPro>();
                        tmp.text = "Configure Spawner";
                        //tmp.color = Color.green;

                        text = foundSpawnerConfirmation.GetTextObject();
                        tmp = text.GetComponent<TextMeshPro>();
                        tmp.text = "Selected spawner " + selectedSpawner.spawnerNumber.ToString();
                        //tmp.color = Color.green;

                        placeSpawnerMode = false;
                    }
                    else
                    {
                        placeSpawnerText.GetComponent<TextMeshPro>().text = "Cannot place spawnpoint here";
                        placeSpawnerText.GetComponent<TextMeshPro>().color = Color.red;
                    }
                    

                }
                //need to disable place spawner mode if you go to the next menu element
            }

            if (setWalkPointMode)
            {
                ghost.transform.position = GetGroundPointFromGameObject(player);
                if (leftHand.controller.GetPrimaryInteractionButtonDown())
                {
                    selectedSpawner.customWalkPoint = GetGroundPointFromGameObject(player);

                    GameObject spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    spawnPoint.transform.position = selectedSpawner.customWalkPoint;
                    spawnPoint.GetComponent<Renderer>().material.color = Color.blue;
                    spawnPoint.GetComponent<Collider>().enabled = false;
                    spawnPoint.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    GameObject.Destroy(placeSpawnerText);
                    ghost.SetActive(false);
                    setWalkPointMode = false;
                }
            }
        }
        
        bool CreateSpawnPoint()
        {
            allPools = UnityEngine.Object.FindObjectsOfType<Pool>();
            if(allPools.Length < 180)
            {
                MelonModLogger.Log("You need to spawn a utility gun");
            }
            Vector3 spawnerPosition = GetGroundPointFromGameObject(player);
            if (spawnerPosition == new Vector3(0f, 0f, 0f))
            {
                return false;
            }
            else
            {
                //building visuals for spawn point
                GameObject spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spawnPoint.GetComponent<Renderer>().material.color = Color.black;
                spawnPoint.GetComponent<Collider>().enabled = false;
                spawnPoint.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                spawnPoint.transform.position = spawnerPosition;
                spawnPoint.transform.eulerAngles = new Vector3(spawnPoint.transform.eulerAngles.x, player.transform.eulerAngles.y, spawnPoint.transform.eulerAngles.z);

                //MelonModLogger.Log("Physical spawner id is " + physicalSpawnPointIDTracker);
                spawnPoint.name = (physicalSpawnPointIDTracker).ToString();

                //creating new spawnpoint class with settings
                Spawner newSpawner = new Spawner();
                InitialiseNewSpawnerValues(newSpawner, spawnerPosition, spawnPoint);

                //UPDATING SELECTED SPAWNER UI
                //GameObject text2 = selectedSpawnerElement.GetTextObject();
                //TextMeshPro tmp = text2.GetComponent<TextMeshPro>();
                //tmp.text = "Selected Spawner " + selectedSpawner.spawnerNumber.ToString();
                //tmp.color = Color.white;

                GameObject spawnPointName = new GameObject("SpawnPointName");
                //CHANGE THIS TO NAME THE SPAWNPOINT BASED ON INDEX RATHER THAN LIST COUNT
                CreateSpawnPointUI(spawnPointName, spawnPoint, new Vector3(0f, 15f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Bold, TextAlignmentOptions.Center, "Spawner " + (activeSpawners.Count + 1).ToString());
                GameObject spawnPointStatus = new GameObject("SpawnPointStatus");
                string status;
                if (newSpawner.spawningActive)
                {
                    status = "Enabled";
                }
                else
                {
                    status = "Disabled";
                }
                CreateSpawnPointUI(spawnPointStatus, spawnPoint, new Vector3(0f, 13f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, "Status: " + status);
                GameObject totalEnemiesSpawned = new GameObject("TotalEnemies");
                CreateSpawnPointUI(totalEnemiesSpawned, spawnPoint, new Vector3(0f, 11f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, "Total Spawned: 0");
                GameObject disableTimeStatus = new GameObject("disableTimeStatus");
                if (newSpawner.useSpawnTimeLimit)
                {
                    status = "Time Left: " + newSpawner.spawnTimeLimit.ToString();
                }
                else
                {
                    status = ("Time Left: Infinite");
                }
                CreateSpawnPointUI(disableTimeStatus, spawnPoint, new Vector3(0f, 9f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, status);
                GameObject disableSpawnStatus = new GameObject("spawnLimitStatus");
                if (newSpawner.useSpawnLimit)
                {
                    status = "Spawns Left: " + newSpawner.spawnLimit.ToString();
                }
                else
                {
                    status = "Spawns Left: Infinite";
                }
                CreateSpawnPointUI(disableSpawnStatus, spawnPoint, new Vector3(0f, 7f, 0f), new Color(0.4622f, 0.4622f, 0.4622f, 1f), FontStyles.Normal, TextAlignmentOptions.Left, status);

                physicalActiveSpawners.Add(spawnPoint);
                activeSpawners.Add(newSpawner);


                return true;
            }

        }

        void InitialiseNewSpawnerValues(Spawner newSpawner, Vector3 spawnerPosition, GameObject spawnPoint)
        {
            newSpawner.spawnerNumber = activeSpawners.Count + 1;//THIS NEEDS TO CHANGE
            newSpawner.spawnPointID = physicalSpawnPointIDTracker;
            physicalSpawnPointIDTracker++;
            //MelonModLogger.Log("Physical Spawner ID increased by 1, now it is " + physicalSpawnPointIDTracker);
            newSpawner.lastUIUpdateTime = 0f;

            //spawn area settings
            newSpawner.position = spawnerPosition;
            newSpawner.customWalkPoint = spawnerPosition;
            newSpawner.rotation = spawnPoint.transform.rotation;

            //newSpawner.useSpawnArea = customSpawnArea.GetValue();
            //newSpawner.spawnAreaSize = new Vector2(spawnAreaX.GetValue(), spawnAreaY.GetValue());

            //spawner activation distance settings
            //newSpawner.useDistanceActivation = enableDistanceStatus.GetValue();
            //newSpawner.distanceToActivate = enableAtDistance.GetValue();

            //spawner disable after time settings
            //newSpawner.useSpawnTimeLimit = disableSpawnerAfterTimeEnabled.GetValue();
            //newSpawner.spawnTimeLimit = disableSpawnerAfterTimeElement.GetValue();

            //spawner disable after spawns settings
            //newSpawner.useSpawnLimit = disableSpawnerAfterSpawnsEnabled.GetValue();
            //newSpawner.spawnLimit = disableSpawnerAfterSpawnsElement.GetValue();

            //newSpawner.maxenemyCount = maxConcurrentAliveElement.GetValue();
            //newSpawner.spawnFrequency = spawnFrequencyElement.GetValue();
            //newSpawner.cleardeadFrequency = deadCleanUpFrequencyElement.GetValue();
            newSpawner.lastSpawntime = 0f;

            //newSpawner.useSpawnLimit = disableSpawnerAfterSpawnsEnabled.GetValue();
            //newSpawner.spawnLimit = disableSpawnerAfterSpawnsElement.GetValue();
            
            //giving spawner default AI Values
            newSpawner.allAISettings = ConstructDefaultAISettings();

            //Setting AI spawning values
            /*
            newSpawner.randomEnemy = randomAIEnabled.GetValue();
            if (nullbodyEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(0);
            }
            if (fordearlyexitEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(1);
            }
            if (omniturretEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(2);
            }
            if (nullbodycorruptedEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(3);
            }
            if (fordearlyexitheadsetEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(4);
            }
            if (fordvrJunkieEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(5);
            }
            if (fordEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(6);
            }
            if (omniwreckerEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(7);
            }
            if (omniprojectorEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(8);
            }
            if (crabletEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(9);
            }
            if (crabletplusEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(10);
            }
            if (nullratEnabled.GetValue())
            {
                newSpawner.enabledEnemies.Add(11);
            }
            */
            selectedSpawner = newSpawner;
        }

        static void CreateSpawnPointUI(GameObject UIObj, GameObject spawnPoint, Vector3 position, Color color, FontStyles fontstyle, TextAlignmentOptions textAlignment, string text)
        {
            CreateText(UIObj, fontstyle, 0.5f, Color.black, color, 0.5f, textAlignment, text, new Vector3(0, 0.07f, 0), new Vector4(9.6f, 9.6f, 9.6f, 9.6f));
            UIObj.transform.SetParent(spawnPoint.transform, false);
            UIObj.transform.localScale = new Vector3(30f, 30f, 1f);
            UIObj.transform.localPosition = position;
        }


        void UpdateSpawnerUI(Spawner spawnPoint)
        {
            if (spawnPoint.UIEnabled)
            {
                foreach (GameObject obj in physicalActiveSpawners)
                {
                    //MelonModLogger.Log("The physical spawnpoint name is " + obj.name + " and the saved spawnpoint id is " + spawnPoint.spawnPointID.ToString());
                    try
                    {
                        if (obj.name == spawnPoint.spawnPointID.ToString())
                        {
                            obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = "Spawner " + spawnPoint.spawnerNumber;

                            string status = "";

                            if (spawnPoint.spawningActive)
                            {
                                status = "Enabled";
                            }
                            else
                            {
                                status = "Disabled";
                            }
                            obj.transform.GetChild(1).GetComponent<TextMeshPro>().text = "Status: " + status;

                            obj.transform.GetChild(2).GetComponent<TextMeshPro>().text = "Total Spawned: " + spawnPoint.totalSpawned.ToString();

                            if (spawnPoint.useSpawnTimeLimit)
                            {
                                if (spawnPoint.spawnTime + spawnPoint.spawnTimeLimit*60 - Time.time <= 0)
                                {
                                    status = "Time Expired";
                                }
                                else
                                {
                                    int rounded = (int)Math.Round(spawnPoint.spawnTime + spawnPoint.spawnTimeLimit*60 - Time.time);
                                    status = "Time Left: " + rounded.ToString();
                                }
                            }
                            else
                            {
                                status = "Time Left: Infinite";
                            }
                            obj.transform.GetChild(3).GetComponent<TextMeshPro>().text = status;

                            if (spawnPoint.useSpawnLimit)
                            {
                                if (spawnPoint.totalSpawned < spawnPoint.spawnLimit)
                                {
                                    status = "Spawns Left: " + (spawnPoint.spawnLimit - spawnPoint.totalSpawned);
                                }
                                else
                                {
                                    status = "Spawns Left: 0";
                                }
                            }
                            else
                            {
                                status = "Spawns Left: Infinite";
                            }
                            obj.transform.GetChild(4).GetComponent<TextMeshPro>().text = status;
                            break;
                        }
                    }
                    catch
                    {
                        //MelonModLogger.Log("Caught failed spawner update attempt");
                    }

                }
            }
        }
            

        void ApplySpawnerSettings(Spawner spawnPoint)
        {
            spawnPoint.spawnAreaSize = new Vector2(spawnAreaX.GetValue(), spawnAreaY.GetValue());

            //spawner activation distance settings
            if (enableDistanceStatus.GetValue())
            {
                spawnPoint.spawningActive = false;
            }
            spawnPoint.useDistanceActivation = enableDistanceStatus.GetValue();
            spawnPoint.distanceToActivate = enableAtDistance.GetValue();
            spawnPoint.UIEnabled = showSpawnerUIElement.GetValue();

            foreach(GameObject obj in physicalActiveSpawners)
            {
                if(obj.name == spawnPoint.spawnPointID.ToString())
                {
                    if (spawnPoint.UIEnabled)
                    {
                        obj.SetActive(true);
                    }
                    else
                    {
                        obj.SetActive(false);
                    }
                    break;
                }
            }

            //spawner disable after time settings
            spawnPoint.useSpawnTimeLimit = disableSpawnerAfterTimeEnabled.GetValue();
            spawnPoint.spawnTimeLimit = disableSpawnerAfterTimeElement.GetValue();

            //spawner disable after spawns settings
            spawnPoint.useSpawnLimit = disableSpawnerAfterSpawnsEnabled.GetValue();
            spawnPoint.spawnLimit = disableSpawnerAfterSpawnsElement.GetValue();



            spawnPoint.maxenemyCount = maxConcurrentAliveElement.GetValue();
            spawnPoint.spawnFrequency = spawnFrequencyElement.GetValue();
            spawnPoint.cleardeadFrequency = deadCleanUpFrequencyElement.GetValue();
            spawnPoint.lastSpawntime = 0f;

            spawnPoint.useSpawnLimit = disableSpawnerAfterSpawnsEnabled.GetValue();
            spawnPoint.spawnLimit = disableSpawnerAfterSpawnsElement.GetValue();
            spawnPoint.movementMode = aiMovementOptionsElement.GetValue();

            //Setting AI spawning values
            spawnPoint.enabledEnemies.Clear();
            spawnPoint.randomEnemy = randomAIEnabled.GetValue();
            if (nullbodyEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(0);
            }
            if (fordearlyexitEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(1);
            }
            if (omniturretEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(2);
            }
            if (nullbodycorruptedEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(3);
            }
            if (fordearlyexitheadsetEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(4);
            }
            if (fordvrJunkieEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(5);
            }
            if (fordEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(6);
            }
            if (omniwreckerEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(7);
            }
            if (omniprojectorEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(8);
            }
            if (crabletEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(9);
            }
            if (crabletplusEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(10);
            }
            if (nullratEnabled.GetValue())
            {
                spawnPoint.enabledEnemies.Add(11);
            }
        }

        void RunSpawnPoints()
        {
            foreach(Spawner spawnPoint in activeSpawners)
            {
                //checking if the distance activation is ready to go
                if (spawnPoint.useDistanceActivation & !spawnPoint.distanceCausedActivation)
                {
                    //MelonModLogger.Log("Spawner " + spawnPoint.spawnerNumber + " is hasd useDistance activation enabled. The player left bool is " + spawnPoint.hasPlayerLeft.ToString() + " and the distance to activate is " + spawnPoint.distanceToActivate + " and the Vector3 distance is " + Vector3.Distance(player.transform.position, spawnPoint.position) + " and spawningActive is set to " + spawnPoint.spawningActive);

                    try
                    {
                        if (!spawnPoint.hasPlayerLeft && Vector3.Distance(player.transform.position, spawnPoint.position) > spawnPoint.distanceToActivate)
                        {
                            spawnPoint.hasPlayerLeft = true;
                        }

                        if (spawnPoint.hasPlayerLeft == true && Vector3.Distance(player.transform.position, spawnPoint.position) < spawnPoint.distanceToActivate)
                        {
                            spawnPoint.spawningActive = true;
                            spawnPoint.distanceCausedActivation = true;
                        }
                    }
                    catch
                    {
                        //MelonModLogger.Log("Caught error related to getting player position");
                    }
                }

                //MelonModLogger.Log("Spawner " + spawnPoint.spawnerNumber + " has a spawningActive value of " + spawnPoint.spawningActive);
                if (!spawnPoint.spawningActive)
                {
                    continue;
                }

                //setting spawn time when distance activation is not enabled
                if (spawnPoint.useSpawnTimeLimit && !spawnPoint.spawnTimeSet)
                {
                    spawnPoint.spawnTime = Time.time;
                    spawnPoint.spawnTimeSet = true;
                }

                //updating spawnpoint UI
                if(Time.time - spawnPoint.lastUIUpdateTime >= 1)
                {
                    spawnPoint.lastUIUpdateTime = Time.time;
                    UpdateSpawnerUI(spawnPoint);
                }
                
                //removing dead enemies from list for spawning new ones
                foreach (AIBrain ai in spawnPoint.spawnedEnemies.ToList())
                {
                    if (ai.isDead == true)
                    {
                        spawnPoint.spawnedEnemies.Remove(ai);
                    }
                }

                //physically clearing dead
                if (Time.time - spawnPoint.lastClearDeadTime > spawnPoint.cleardeadFrequency)
                {
                    //MelonModLogger.Log("Spawner " + spawnPoint.spawnerNumber + " is clearing their dead. Last clear time was " + spawnPoint.lastClearDeadTime + " making the subtraction calc result " + (Time.time - spawnPoint.lastClearDeadTime).ToString());
                    spawnPoint.lastClearDeadTime = Time.time;
                    foreach (AIBrain ai in spawnPoint.spawnedEnemiesIncludingDead.ToList())
                    {
                        if (ai.isDead == true)
                        {
                            UnityEngine.Object.Destroy(ai.gameObject);
                            spawnPoint.spawnedEnemiesIncludingDead.Remove(ai);
                        }
                    }
                }

                //switching off spawner if it exceeds spawn limit or time limit
                if(spawnPoint.useSpawnLimit==true && spawnPoint.totalSpawned >= spawnPoint.spawnLimit || spawnPoint.useSpawnTimeLimit && Time.time > spawnPoint.spawnTime + spawnPoint.spawnTimeLimit*60)
                {
                    spawnPoint.spawningActive = false;
                    UpdateSpawnerUI(spawnPoint);
                    foreach (AIBrain ai in spawnPoint.spawnedEnemiesIncludingDead.ToList())
                    {
                        if (ai.isDead == true)
                        {
                            UnityEngine.Object.Destroy(ai.gameObject);
                            spawnPoint.spawnedEnemiesIncludingDead.Remove(ai);
                        }
                    }
                }
                //MelonModLogger.Log("For spawner " + spawnPoint.spawnerNumber + " the enemy spawn count is " + spawnPoint.spawnedEnemies.Count + " and the max enemy counts is " + spawnPoint.maxenemyCount);
                if (spawnPoint.spawnedEnemies.Count < spawnPoint.maxenemyCount)
                {
                    //MelonModLogger.Log("For spawner " + spawnPoint.spawnerNumber + " the time difference is " + (Time.time - spawnPoint.lastSpawntime).ToString() + " and the spawn freq is " + spawnPoint.spawnFrequency);
                    if (Time.time - spawnPoint.lastSpawntime > spawnPoint.spawnFrequency)
                    {
                        string enemyToSpawnPool = "";
                        int enemyToSpawnNumber = 0;
                        spawnPoint.lastSpawntime = Time.time;
                        if (spawnPoint.randomEnemy)
                        {
                            int number = UnityEngine.Random.Range(0, 12);
                            enemyToSpawnNumber = number;
                            switch (number)
                            {
                                case 0:
                                    enemyToSpawnPool = "pool - Null Body";
                                    break;
                                case 1:
                                    enemyToSpawnPool = "pool - Ford EarlyExit";//I GUESSED, MIGHT CAUSE ISSUES
                                    break;
                                case 2:
                                    enemyToSpawnPool = "pool - OmniTurret";
                                    break;
                                case 3:
                                    enemyToSpawnPool = "pool - Null Body Corrupted";
                                    break;
                                case 4:
                                    enemyToSpawnPool = "pool - Ford Early Exit Headset";
                                    break;
                                case 5:
                                    enemyToSpawnPool = "pool - Ford VR Junkie";
                                    break;
                                case 6:
                                    enemyToSpawnPool = "pool - Ford";
                                    break;
                                case 7:
                                    enemyToSpawnPool = "pool - Omni Wrecker";
                                    break;
                                case 8:
                                    enemyToSpawnPool = "pool - Omni Projector";
                                    break;
                                case 9:
                                    enemyToSpawnPool = "pool - Crablet";
                                    break;
                                case 10:
                                    enemyToSpawnPool = "pool - Crablet Plus";
                                    break;
                                case 11:
                                    enemyToSpawnPool = "pool - Null Rat";
                                    break;
                            }
                        }
                        else
                        {
                            int enemyNumber = spawnPoint.enabledEnemies[UnityEngine.Random.Range(0, spawnPoint.enabledEnemies.Count)];
                            enemyToSpawnNumber = enemyNumber;
                            switch (enemyNumber)
                            {
                                case 0:
                                    enemyToSpawnPool = "pool - Null Body";
                                    break;
                                case 1:
                                    enemyToSpawnPool = "pool - Ford EarlyExit";//I GUESSED, MIGHT CAUSE ISSUES
                                    break;
                                case 2:
                                    enemyToSpawnPool = "pool - OmniTurret";
                                    break;
                                case 3:
                                    enemyToSpawnPool = "pool - Null Body Corrupted";
                                    break;
                                case 4:
                                    enemyToSpawnPool = "pool - Ford Early Exit Headset";
                                    break;
                                case 5:
                                    enemyToSpawnPool = "pool - Ford VR Junkie";
                                    break;
                                case 6:
                                    enemyToSpawnPool = "pool - Ford";
                                    break;
                                case 7:
                                    enemyToSpawnPool = "pool - Omni Wrecker";
                                    break;
                                case 8:
                                    enemyToSpawnPool = "pool - Omni Projector";
                                    break;
                                case 9:
                                    enemyToSpawnPool = "pool - Crablet";
                                    break;
                                case 10:
                                    enemyToSpawnPool = "pool - Crablet Plus";
                                    break;
                                case 11:
                                    enemyToSpawnPool = "pool - Null Rat";
                                    break;
                            }

                        }
                        //MelonModLogger.Log("Spawner " + spawnPoint.spawnerNumber + " is spawning enemy from pool " + enemyToSpawnPool);
                        SpawnEnemy(enemyToSpawnPool, spawnPoint, spawnPoint.allAISettings[enemyToSpawnNumber]);
                    }
                }
            }
        }

        void SpawnEnemy(string EnemyTypePool, Spawner spawnPoint, AISettings thisAISettings)
        {
            foreach(Pool p in allPools)
            {
                try
                {
                    if (p.name == EnemyTypePool)
                    {
                        Vector3 spawnPos = spawnPoint.position;
                        if (spawnPoint.spawnAreaSize.x != 0 || spawnPoint.spawnAreaSize.y != 0)
                        {
                            spawnPos = RandomSpawnPoint(spawnPoint.position, spawnPoint.spawnAreaSize, spawnPoint);
                        }
                        GameObject spawnedAI = UnityEngine.Object.Instantiate(p.Prefab, spawnPos, Quaternion.identity);
                        spawnPoint.totalSpawned++;

                        AIBrain spawnedAIBrain = spawnedAI.GetComponent<AIBrain>();
                        BehaviourBaseNav behaviour = spawnedAIBrain.behaviour;
                        behaviour.health.cur_hp = thisAISettings.aiHealth / 100;
                        behaviour.roamSpeed = thisAISettings.roamSpeed;
                        behaviour.roamAngSpeed = thisAISettings.roamAngularSpeed;
                        behaviour.roamRange = new Vector2(thisAISettings.roamRangeX, thisAISettings.roamRangeY);
                        behaviour.agroedSpeed = thisAISettings.agroedSpeed;
                        behaviour.agroedAngSpeed = thisAISettings.agroedAngularSpeed;
                        behaviour.investigateRange = thisAISettings.investigationRange;
                        behaviour.breakAgroTargetDistance = thisAISettings.breakAgroTargetDistance;
                        behaviour.breakAgroHomeDistance = thisAISettings.breakAgroHomeDistance;
                        behaviour.sensors.visionFov = thisAISettings.visionFOV;
                        behaviour.sensors.hearingSensitivity = thisAISettings.hearingSensitivity;
                        behaviour.sensors.additionalMass = thisAISettings.additionalMass;
                        if (thisAISettings.crabletBaseColor == "Random")
                        {
                            behaviour.baseColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
                        }
                        else
                        {
                            behaviour.baseColor = thisAISettings.defaultcrabletBaseColor;
                        }

                        if (thisAISettings.crabletAgroedColor == "Random")
                        {
                            behaviour.agroColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
                        }
                        else
                        {
                            behaviour.agroColor = thisAISettings.defaultcrabletAgroedColor;
                        }
                        behaviour.health.aggression = thisAISettings.aggression;
                        behaviour.health.irritability = thisAISettings.irritability;
                        behaviour.health.placability = thisAISettings.placeability;
                        behaviour.health.vengefulness = thisAISettings.vengefulness;
                        behaviour.health.stunRecovery = thisAISettings.stunRecoveryTime;
                        behaviour.health.maxStunSeconds = thisAISettings.maxStunTime;
                        behaviour.health.minHeadImpact = thisAISettings.minHeadImpact;
                        behaviour.health.minSpineImpact = thisAISettings.minSpineImpact;
                        behaviour.health.minLimbImpact = thisAISettings.minLimbImpact;
                        behaviour.restingRange = thisAISettings.restingRange;
                        behaviour.freezeWhileResting = thisAISettings.freezeWhileResting;
                        behaviour.homeIsPost = thisAISettings.homeIsPost;
                        behaviour.activeRange = thisAISettings.activeRange;
                        behaviour.roamFrequency = thisAISettings.roamFrequency;
                        behaviour.roamWanders = thisAISettings.roamWander;
                        behaviour.enableThrowAttack = thisAISettings.enableThrowAttack;
                        behaviour.throwMaxRange = thisAISettings.throwAttackMaxRange;
                        behaviour.throwMinRange = thisAISettings.throwAttackMinRange;
                        behaviour.throwCooldown = thisAISettings.throwCooldown;
                        behaviour.throwVelocity = thisAISettings.throwVelocity;
                        behaviour.gunRange = thisAISettings.gunRange;
                        behaviour.gunCooldown = thisAISettings.gunCooldown;
                        behaviour.accuracy = thisAISettings.gunAccuracy;
                        behaviour.reloadTime = thisAISettings.reloadTime;
                        behaviour.clipSize = thisAISettings.clipSize;
                        behaviour.burstSize = thisAISettings.burstSize;
                        behaviour.desiredGunDistance = thisAISettings.desiredGunDistance;

                        switch (spawnPoint.movementMode)
                        {
                            case "Stationary":
                                break;
                            case "Walk to Custom Point":
                                behaviour.SwitchMentalState(BehaviourBaseNav.MentalState.Investigate);
                                behaviour.SetPath(spawnPoint.customWalkPoint);//Makes AI walk to that point
                                break;
                            case "Agro to Player":
                                behaviour.SwitchMentalState(BehaviourBaseNav.MentalState.Agroed);
                                behaviour.SetAgro(rightHand.triggerRefProxy);
                                break;
                            case "Roam":
                                behaviour.SwitchMentalState(BehaviourBaseNav.MentalState.Roam);
                                behaviour.SetRoam(new Vector2(spawnPoint.spawnAreaSize.x, spawnPoint.spawnAreaSize.y), thisAISettings.roamSpeed, true, 100f);
                                break;
                        }


                        //TESTING AI MOVEMENT
                        //behaviour.SwitchMentalState(BehaviourBaseNav.MentalState.Roam);
                        //behaviour.SetRoam(new Vector2(spawnPoint.spawnAreaSize.x, spawnPoint.spawnAreaSize.y), thisAISettings.roamSpeed, false, 1f);//makes AI explore within defined Vector2 area. If roam wanders enabled new area will be considered each time they reach their destination

                        //behaviour.SetPath(AIPathTarget);//Makes AI walk to that point
                        //behaviour.SetAgro(rightHand.triggerRefProxy);
                        /*
                        MelonModLogger.Log("The mental state is " + Enum.GetName(typeof(PuppetMasta.BehaviourBaseNav.MentalState), spawnedAIBrain.behaviour.mentalState));
                        spawnedAIBrain.behaviour.sensors.visionFov = 360f;
                        spawnedAIBrain.behaviour.activeRange = 100f;
                        spawnedAIBrain.behaviour.roamRange = new Vector2(500f, 500f);
                        spawnedAIBrain.behaviour.roamWanders = true;
                        spawnedAIBrain.behaviour.investigateRange = 1000f;
                        spawnedAIBrain.behaviour.breakAgroHomeDistance = 1000f;
                        spawnedAIBrain.behaviour.breakAgroTargetDistance = 1000f;
                        spawnedAIBrain.behaviour.SwitchMentalState(PuppetMasta.BehaviourBaseNav.MentalState.Roam);
                        MelonModLogger.Log("The new mental state is " + Enum.GetName(typeof(PuppetMasta.BehaviourBaseNav.MentalState), spawnedAIBrain.behaviour.mentalState));
                        */
                        spawnPoint.spawnedEnemies.Add(spawnedAIBrain);
                        spawnPoint.spawnedEnemiesIncludingDead.Add(spawnedAIBrain);

                    }
                }
                catch
                {
                   // MelonModLogger.Log("Caught weird pool name error");
                }
                
            }
        }    

        Vector3 GetGroundPointFromGameObject(GameObject player)
        {
            RaycastHit[] hits;
            Vector3 returnVector = new Vector3(0, 0, 0);
            hits = Physics.RaycastAll(player.transform.position + 2*player.transform.forward + 2*player.transform.up, -Vector3.up, 100f);
            /*
            foreach (RaycastHit hitObject in hits)
            {
                returnVector = hitObject.point;
            }
            */
            if(hits.Length > 0)
            {
                returnVector = hits[0].point;
            }
            return returnVector;
        }

        Vector3 RandomSpawnPoint(Vector3 spawnerPosition, Vector2 spawnAreaSize, Spawner spawnPoint)
        {
            RaycastHit[] hits;
            Vector3 returnVector = spawnerPosition;
            hits = Physics.RaycastAll(spawnerPosition + spawnPoint.maxHeight * Vector3.up + new Vector3(UnityEngine.Random.Range(-(spawnAreaSize.x / 2), spawnAreaSize.x / 2), 0f, UnityEngine.Random.Range(-(spawnAreaSize.y / 2), spawnAreaSize.y / 2)), -Vector3.up, 100f);
            if (hits.Length > 0)
            {
                returnVector = hits[0].point;
            }
            return returnVector;
        }

        void VisualiseSpawnArea(Vector3 spawnerPosition, Vector2 spawnAreaSize)
        {
            ApplySpawnerSettings(selectedSpawner);
            selectedSpawner.spawnAreaVisualEnabled = !selectedSpawner.spawnAreaVisualEnabled;
            if (selectedSpawner.spawnAreaVisualEnabled)
            {
                spawnAreaVisual = new GameObject("SpawnArea");

                for (float x = -spawnAreaSize.x / 2; x <= spawnAreaSize.x / 2; x = x + 0.5f)
                {
                    RaycastHit[] hits;
                    Vector3 position = spawnerPosition;
                    hits = Physics.RaycastAll(spawnerPosition + selectedSpawner.maxHeight * Vector3.up + Vector3.right * x, -Vector3.up, 100f);
                    if (hits.Length > 0)
                    {
                        position = hits[0].point;
                    }
                    GameObject newCube = UnityEngine.Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), position, Quaternion.identity);
                    newCube.GetComponent<Renderer>().material.color = Color.red;
                    newCube.GetComponent<Collider>().enabled = false;
                    newCube.transform.SetParent(spawnAreaVisual.transform);
                    newCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    //
                }

                for (float y = -spawnAreaSize.y / 2; y <= spawnAreaSize.y / 2; y = y + 0.5f)
                {
                    RaycastHit[] hits;
                    Vector3 position = spawnerPosition;
                    hits = Physics.RaycastAll(spawnerPosition + 10 * Vector3.up + Vector3.forward * y, -Vector3.up, 100f);
                    if (hits.Length > 0)
                    {
                        position = hits[0].point;
                    }
                    GameObject newCube = UnityEngine.Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), position, Quaternion.identity);
                    newCube.GetComponent<Renderer>().material.color = Color.red;
                    newCube.GetComponent<Collider>().enabled = false;
                    newCube.transform.SetParent(spawnAreaVisual.transform);
                    newCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    //newCube.transform.SetParent(spawnAreaVisualyBar.transform);
                }
            }
            else
            {
                GameObject.Destroy(spawnAreaVisual);
            }
        }

        void VisualiseSpawnDistance(Vector3 spawnerPosition, float distance)
        {
            ApplySpawnerSettings(selectedSpawner);
            selectedSpawner.distanceAreaVisualEnabled = !selectedSpawner.distanceAreaVisualEnabled;
            if (selectedSpawner.distanceAreaVisualEnabled)
            {
                distanceVisual = UnityEngine.Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Sphere), spawnerPosition, Quaternion.identity);
                distanceVisual.GetComponent<Collider>().enabled = false;
                Material material = new Material(Shader.Find("Standard"));
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.color = new Color(0, 1, 0, 0.6f);
                distanceVisual.GetComponent<Renderer>().material = material;
                
                distanceVisual.transform.localScale = new Vector3(distance*2, distance*2, distance*2);
            }
            else
            {
                GameObject.Destroy(distanceVisual);
            } 
        }

        void VisualiseSpawnHeight(Vector3 spawnerPosition, float height)
        {
            ApplySpawnerSettings(selectedSpawner);
            selectedSpawner.heightVisualEnabled = !selectedSpawner.heightVisualEnabled;
            if (selectedSpawner.heightVisualEnabled)
            {
                heightVisual = UnityEngine.Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), spawnerPosition, Quaternion.identity);
                heightVisual.GetComponent<Collider>().enabled = false;
                Material material = new Material(Shader.Find("Standard"));
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.color = new Color(0, 0, 1, 0.6f);
                heightVisual.GetComponent<Renderer>().material = material;
                heightVisual.transform.localScale = new Vector3(0.5f, height*2, 0.5f);
            }
            else
            {
                GameObject.Destroy(heightVisual);
            }
        }

        MenuInterface mainMenu;
        MenuInterface configureSpawnerInterface;
        MenuInterface spawnerSettings;
        MenuInterface spawnerSettings2;
        MenuInterface AISettings;
        MenuInterface spawnArea;
        MenuInterface spawnEnableDistance;
        MenuInterface spawnHeight;
        MenuInterface disableSpawnerAfterTimeInterface;
        MenuInterface disableSpawnerAfterSpawnsInterface;
        MenuInterface AISelectionInterface1;
        MenuInterface AISelectionInterface2;
        MenuInterface AISelectionInterface3;
        MenuInterface AISettingsMenuInterface;
        MenuInterface ConfigureAISettings1;
        MenuInterface ConfigureAISettings2;
        MenuInterface ConfigureAISettings3;
        MenuInterface ConfigureAISettings4;
        MenuInterface ConfigureAISettings5;
        MenuInterface ConfigureAISettings6;
        MenuInterface ConfigureAISettings7;
        MenuInterface ConfigureAISettings8;
        MenuInterface ConfigureAISettings9;
        MenuInterface aiMovementInterface;

        MenuElement placeNewSpawnerElement;
        MenuElement findSpawnerElement;
        MenuElement foundSpawnerConfirmation;
        MenuElement configureSpawnerElement;
        MenuElement selectedSpawnerElement;
        MenuElement toggleSpawnerElement;
        MenuElement saveSpawnerElement;
        MenuElement deleteSpawnerElement;
        MenuElement spawnerSettingsElement;
        MenuElement aiSettingsElement;
        MenuElement setCustomSpawnAreaElement;
        MenuElement setEnableAtDistanceElement;
        MenuElement setSpawnHeightElement;
        MenuElement disableSpawnerAfterTimeCategoryElement;
        MenuElement disableSpawnerAfterSpawnsCategoryElement;
        MenuElement selectAItoSpawnCategory;
        MenuElement configureAISettingsCategory;
        MenuElement spawnerSettingsNextPage;
        MenuElement nextPage1;
        MenuElement nextPage2;
        MenuElement nextPage3;
        MenuElement nextPage4;
        MenuElement nextPage5;
        MenuElement nextPage6;
        MenuElement nextPage7;
        MenuElement nextPage8;
        MenuElement nextPage9;
        MenuElement nextPage10;
        MenuElement configureAISettingsCategory2;
        MenuElement configureAIMovementCategory;

        //AI Movement
        StringElement aiMovementOptionsElement;
        MenuElement setCustomWalkPoint;

        //spawn area
        //BoolElement customSpawnArea;
        FloatElement spawnAreaX;
        FloatElement spawnAreaY;
        MenuElement visualiseSpawnArea;

        //spawn enable distance
        BoolElement enableDistanceStatus;
        FloatElement enableAtDistance;
        MenuElement visualiseEnableDistance;

        //spawn height
        FloatElement spawnHeightElement;
        MenuElement visualiseSpawnHeightElement;

        BoolElement showSpawnerUIElement;

        FloatElement spawnFrequencyElement;
        FloatElement deadCleanUpFrequencyElement;
        IntElement maxConcurrentAliveElement;

        //Remove spawner after time
        BoolElement disableSpawnerAfterTimeEnabled;
        FloatElement disableSpawnerAfterTimeElement;

        //Disable spawner after spawns
        BoolElement disableSpawnerAfterSpawnsEnabled;
        IntElement disableSpawnerAfterSpawnsElement;

        //AI Selection Interface
        BoolElement randomAIEnabled;
        BoolElement nullbodyEnabled;
        BoolElement fordearlyexitEnabled;
        BoolElement omniturretEnabled;
        BoolElement nullbodycorruptedEnabled;
        BoolElement fordearlyexitheadsetEnabled;
        BoolElement fordvrJunkieEnabled;
        BoolElement fordEnabled;
        BoolElement omniwreckerEnabled;
        BoolElement omniprojectorEnabled;
        BoolElement crabletEnabled;
        BoolElement crabletplusEnabled;
        BoolElement nullratEnabled;

        //AI Settings Menu
        StringElement selectedAIElement;
        //MenuElement loadSelectedAISettingsElement;
        //MenuElement resetToDefaultElement;
        //BoolElement enableRandomModeElement;
        MenuElement applySettingsToAIElement;

        //AI Settings WTF
        FloatElement aiHealthElement;
        FloatElement roamSpeedElement;
        FloatElement roamAngularSpeedElement;
        FloatElement roamRangeXElement;
        FloatElement roamRangeYElement;
        FloatElement agroedSpeedElement;
        FloatElement agroedAngularSpeedElement;
        FloatElement investigationRangeElement;
        FloatElement breakAgroTargetDistanceElement;
        FloatElement breakAgroHomeDistanceElement;
        FloatElement visionFOVElement;
        FloatElement hearingSensitivityElement;
        FloatElement AdditionalMassElement;
        StringElement crabletBaseColorElement;
        StringElement crabletAgroedColorElement;
        FloatElement aggressionElement;
        FloatElement irritabiltyElement;
        FloatElement placeabilityElement;
        FloatElement vengefulnessElement;
        FloatElement stunRecoveryTimeElement;
        FloatElement maxStunTimeElement;
        FloatElement minHeadImpactElement;
        FloatElement minSpineImpactElement;
        FloatElement minLimbImpactElement;
        FloatElement restingRangeElement;
        BoolElement freezeWhileRestingElement;
        BoolElement homeIsPostElement;
        FloatElement activeRangeElement;
        FloatElement roamFrequencyElement;
        BoolElement roamWanderElement;
        BoolElement enableThrowAttackElement;
        FloatElement throwAttackMaxRangeElement;
        FloatElement throwAttackMinRangeElement;
        FloatElement throwCooldownElement;
        FloatElement throwVelocityElement;
        FloatElement gunRangeElement;
        FloatElement gunCooldownElement;
        FloatElement gunAccuracyElement;
        FloatElement reloadTimeElement;
        IntElement clipSizeElement;
        IntElement burstSizeElement;
        FloatElement desiredGunDistanceElement;

        //Color textColor = new Color(0.4622f, 0.4622f, 0.4622f, 1f);
        //Color textColor = new Color(0.1981132f, 0.1981132f, 0.1981132f);
        Color textColor = Color.black;
        Color titleColor = Color.black;

        void CreateUI()
        {
            List<string> colorOptions = new List<string>();
            colorOptions.Add("Default");
            colorOptions.Add("Random");

            List<string> movementOptions = new List<string>();
            movementOptions.Add("Stationary");
            movementOptions.Add("Walk to Custom Point");
            movementOptions.Add("Agro to Player");
            movementOptions.Add("Roam");

            allAI.Add("Null Body");
            allAI.Add("Ford Early Exit");
            allAI.Add("Omniturret");
            allAI.Add("Null Body Corrupted");
            allAI.Add("Ford Early Exit Headset");
            allAI.Add("Ford VR Junkie");
            allAI.Add("Ford");
            allAI.Add("Omni Wrecker");
            allAI.Add("Omni Projector");
            allAI.Add("Crablet");
            allAI.Add("Crablet Plus");
            allAI.Add("Null Rat");
            allAI.Add("All");

            //Interfaces
            mainMenu = Interfaces.AddNewInterface("EnemySpawner", titleColor);
            configureSpawnerInterface = Interfaces.CreateCategoryInterface("Configure Spawner", titleColor);
            spawnerSettings = Interfaces.CreateCategoryInterface("Spawner Settings Page 1", titleColor);
            spawnerSettings2 = Interfaces.CreateCategoryInterface("Spawner Settings Page 2", titleColor);
            AISettings = Interfaces.CreateCategoryInterface("AI Settings", titleColor);
            spawnArea = Interfaces.CreateCategoryInterface("Custom Spawn Area", titleColor);
            spawnEnableDistance = Interfaces.CreateCategoryInterface("Spawner Enable Distance", titleColor);
            spawnHeight = Interfaces.CreateCategoryInterface("Max Spawn Ground Height", titleColor);
            disableSpawnerAfterTimeInterface = Interfaces.CreateCategoryInterface("Disable Spawner After Time", titleColor);
            disableSpawnerAfterSpawnsInterface = Interfaces.CreateCategoryInterface("Disable Spawner After Spawns", titleColor);
            aiMovementInterface = Interfaces.CreateCategoryInterface("AI Movement", titleColor);
            AISelectionInterface1 = Interfaces.CreateCategoryInterface("Select AI To Spawn Page 1", titleColor);
            AISelectionInterface2 = Interfaces.CreateCategoryInterface("Select AI To Spawn Page 2", titleColor);
            AISelectionInterface3 = Interfaces.CreateCategoryInterface("Select AI To Spawn Page 3", titleColor);
            AISettingsMenuInterface = Interfaces.CreateCategoryInterface("Configure AI Settings", titleColor);
            ConfigureAISettings1 = Interfaces.CreateCategoryInterface("AI Settings Page 1", titleColor);
            ConfigureAISettings2 = Interfaces.CreateCategoryInterface("AI Settings Page 2", titleColor);
            ConfigureAISettings3 = Interfaces.CreateCategoryInterface("AI Settings Page 3", titleColor);
            ConfigureAISettings4 = Interfaces.CreateCategoryInterface("AI Settings Page 4", titleColor);
            ConfigureAISettings5 = Interfaces.CreateCategoryInterface("AI Settings Page 5", titleColor);
            ConfigureAISettings6 = Interfaces.CreateCategoryInterface("AI Settings Page 6", titleColor);
            ConfigureAISettings7 = Interfaces.CreateCategoryInterface("AI Settings Page 7", titleColor);
            ConfigureAISettings8 = Interfaces.CreateCategoryInterface("AI Settings Page 8", titleColor);
            ConfigureAISettings9 = Interfaces.CreateCategoryInterface("AI Settings Page 9", titleColor);

            //Main Menu
            placeNewSpawnerElement = mainMenu.CreateFunctionElement("Place New Spawner", textColor, null, null, delegate { PlaceSpawnerMode(); }, "Creates new spawner with the settings currently setup");
            findSpawnerElement = mainMenu.CreateFunctionElement("Select Spawner", textColor, null, null, delegate { FindSpawner(); }, "Stand over a spawner and press this button to select it");
            foundSpawnerConfirmation = mainMenu.CreateFunctionElement("No Spawner Found", textColor, null, null, delegate { Nothing(); }, "Make sure the spawner here is the one you want to modify");
            configureSpawnerElement = mainMenu.CreateCategoryElement("Configure Spawner", textColor, configureSpawnerInterface, delegate { LoadSpawnerSettingsInUI(); }, "After finding the spawner, click here to modify that spawner's configuration");

            //Configure Spawner
            selectedSpawnerElement = configureSpawnerInterface.CreateFunctionElement(("No Spawner Selected"), Color.red, null, null, delegate { Nothing(); }, "The spawner you are configuring");
            toggleSpawnerElement = configureSpawnerInterface.CreateFunctionElement("Toggle Spawner Activation", textColor, null, null, delegate { ToggleSpawnPointActivation(selectedSpawner); }, "Turn the spawner on or off. The spawner starts off. If activate at distance is enabled you it will only activate when you walk away then come back");
            saveSpawnerElement = configureSpawnerInterface.CreateFunctionElement("Save Configuration", textColor, null, null, delegate { SaveSpawnerSettings(selectedSpawner); }, "Saves all spawn settings. Press this after changing stuff");
            deleteSpawnerElement = configureSpawnerInterface.CreateFunctionElement("Delete Spawner", Color.red, null, null, delegate { DeleteSelectedSpawner(); }, "Deletes the spawner currently selected");
            spawnerSettingsElement = configureSpawnerInterface.CreateCategoryElement("Spawner Settings", textColor, spawnerSettings, null, "Configure spawner-related settings");
            aiSettingsElement = configureSpawnerInterface.CreateCategoryElement("AI Settings", textColor, AISettings, null, "Configure AI-related settings");

            //Spawner settings
            maxConcurrentAliveElement = spawnerSettings.CreateIntElement("Max Enemies", textColor, 1, 1000, 1, 10, "", "The max number of alive enemies that can exist at once");
            spawnFrequencyElement = spawnerSettings.CreateFloatElement("Spawn Frequency", textColor, 0f, 1000f, 1f, 5f, 0, "seconds", "How frequently new AI will spawn");
            deadCleanUpFrequencyElement = spawnerSettings.CreateFloatElement("Dead Removal", textColor, 0f, 1000f, 1f, 30f, 0, "seconds", "How frequently dead AI are removed. Helps maintain good performance over long periods of time");
            setCustomSpawnAreaElement = spawnerSettings.CreateCategoryElement("Set Custom Spawn Area", textColor, spawnArea, null, "Set a custom area for enemies to randomly spawn somewhere within");
            spawnAreaX = spawnArea.CreateFloatElement("Area X Width", textColor, 0f, 1000f, 1f, 10f, 1, "", "Width of the spawn area along X axis");
            spawnAreaY = spawnArea.CreateFloatElement("Area Z Width", textColor, 0f, 1000f, 1f, 10f, 1, "", "Width of the spawn area along Z axis");
            visualiseSpawnArea = spawnArea.CreateFunctionElement("Toggle Spawn Area Visual", textColor, null, null, delegate { VisualiseSpawnArea(selectedSpawner.position, new Vector2(spawnAreaX.GetValue(), spawnAreaY.GetValue())); }, "Toggle squares visual showing how big the spawn area is. The spawn area is a square with the width and height of the visualised squares");
            spawnerSettingsNextPage = spawnerSettings.CreateCategoryElement("Next Page", textColor, spawnerSettings2);

            setEnableAtDistanceElement = spawnerSettings2.CreateCategoryElement("Set Activate Distance", textColor, spawnEnableDistance, null, "Only activate the spawner when you come within a certain distance of it. Useful for creating scripted enemy spawns when saving the spawner in a scene");
            enableDistanceStatus = spawnEnableDistance.CreateBoolElement("Enabled", textColor, false, "Enable or disable spawner activation within a certain distance");
            enableAtDistance = spawnEnableDistance.CreateFloatElement("Distance", textColor, 1f, 1000f, 1f, 5f, 0, " ", "The spawner will activate when the player comes within this distance. Player must have left the distance area first though");
            visualiseEnableDistance = spawnEnableDistance.CreateFunctionElement("Toggle Distance Visual", textColor, null, null, delegate { VisualiseSpawnDistance(selectedSpawner.position, enableAtDistance.GetValue()); }, "Show or hide visual showing the area that the player must be within to activate the spawner after having left the area");
            setSpawnHeightElement = spawnerSettings2.CreateCategoryElement("Set Max Surface Spawn Height", textColor, spawnHeight, "Set the maximum ground height within which enemies will be able to spawn on. Useful for forcing enemies to spawn in a room with a low ceiling for example");
            //customSpawnArea = spawnArea.CreateBoolElement("Enabled", textColor, false, "Enable or disable use of the custom spawn area");
            spawnHeightElement = spawnHeight.CreateFloatElement("Max Height", textColor, 1f, 1000f, 1f, 10f, 0, "", "Height within which enemies can spawn");
            visualiseSpawnHeightElement = spawnHeight.CreateFunctionElement("Toogle Spawn Height Visual", textColor, null, null, delegate { VisualiseSpawnHeight(selectedSpawner.position, spawnHeightElement.GetValue()); }, "Show or hide visual showing spawn height. Should be where the spawn point is");
            disableSpawnerAfterTimeCategoryElement = spawnerSettings2.CreateCategoryElement("Spawner Lifetime", textColor, disableSpawnerAfterTimeInterface, null, "Disable the spawner after a certain amount of time at which point it will stop spawning new enemies");
            disableSpawnerAfterTimeEnabled = disableSpawnerAfterTimeInterface.CreateBoolElement("Enabled", textColor, false, "Enable or disable spawner time limit");
            disableSpawnerAfterTimeElement = disableSpawnerAfterTimeInterface.CreateFloatElement("Time To Disable After", textColor, 0.5f, 1000f, 0.5f, 1f, 1, "minute(s)", "How long the spawner takes to disable");
            disableSpawnerAfterSpawnsCategoryElement = spawnerSettings2.CreateCategoryElement("Disable Spawner After Spawns", textColor, disableSpawnerAfterSpawnsInterface, null, "Disable the spawner after a certain number of enemies have been spawned");
            disableSpawnerAfterSpawnsEnabled = disableSpawnerAfterSpawnsInterface.CreateBoolElement("Enabled", textColor, false, "Enable or disable spawner enemy limit");
            disableSpawnerAfterSpawnsElement = disableSpawnerAfterSpawnsInterface.CreateIntElement("Spawn Limit", textColor, 1, 1000, 1, 20, "", "Spawner will disable after this many enemies have been spawned");
            showSpawnerUIElement = spawnerSettings2.CreateBoolElement("Enable Spawner UI", textColor, true, "Hide or show the UI above the spawnpoint as well as the physical spawnpoint itself");

            

            //AISettings
            selectAItoSpawnCategory = AISettings.CreateCategoryElement("Select AI To Spawn", textColor, AISelectionInterface1);
            configureAISettingsCategory = AISettings.CreateCategoryElement("Configure AI Settings", textColor, AISettingsMenuInterface);
            configureAIMovementCategory = AISettings.CreateCategoryElement("Configure AI Movement", textColor, aiMovementInterface);

            //AI Movement
            aiMovementOptionsElement = aiMovementInterface.CreateStringElement("Movement Mode", textColor, movementOptions, "Agro to Player", "Agro to Player: AI will spawn in and instantly walk to player and attack. Roam: AI will walk around randomly through the map. Stationary: AI will spawn in and not move. Walk to Custom Point: AI will spawn in and instantly walk to the custom point you set");
            setCustomWalkPoint = aiMovementInterface.CreateFunctionElement("Set Custom Walk Point", textColor, null, null, delegate { SetWalkPointMode(); }, "Set the custom walk point. Make sure the option above it set to Walk to Point otherwise this will not work");

            //ChooseAI Page 1
            randomAIEnabled = AISelectionInterface1.CreateBoolElement("Spawn Random AI", textColor, true, "Each time an enemy is spawned, it will be a random one from all the enemies in the game");
            nullbodyEnabled = AISelectionInterface1.CreateBoolElement("Null Body", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            fordearlyexitEnabled = AISelectionInterface1.CreateBoolElement("Ford Early Exit", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            omniturretEnabled = AISelectionInterface1.CreateBoolElement("Omniturret", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            nullbodycorruptedEnabled = AISelectionInterface1.CreateBoolElement("Null Body Corrupted", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            nextPage1 = AISelectionInterface1.CreateCategoryElement("Next Page", textColor, AISelectionInterface2);
            //Page 2
            fordearlyexitheadsetEnabled = AISelectionInterface2.CreateBoolElement("Ford Early Exit Headset", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            fordvrJunkieEnabled = AISelectionInterface2.CreateBoolElement("Ford VR Junkie", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            fordEnabled = AISelectionInterface2.CreateBoolElement("Ford", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            omniwreckerEnabled = AISelectionInterface2.CreateBoolElement("Omni Wrecker", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            omniprojectorEnabled = AISelectionInterface2.CreateBoolElement("Omni Projector", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            nextPage2 = AISelectionInterface2.CreateCategoryElement("Next Page", textColor, AISelectionInterface3);
            //Page 3
            crabletEnabled = AISelectionInterface3.CreateBoolElement("Crablet", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            crabletplusEnabled = AISelectionInterface3.CreateBoolElement("Crablet Plus", textColor, false, "If you enable multiple enemies from this list, each spawn will be a random enemy out of the ones you have enabled");
            nullratEnabled = AISelectionInterface3.CreateBoolElement("Null Rat", textColor, false);

            //AI Settings Menu
            selectedAIElement = AISettingsMenuInterface.CreateStringElement("AI Selected", textColor, allAI, "Null Body", "The AI whose settings will be modified");
            //loadSelectedAISettingsElement = AISettingsMenuInterface.CreateFunctionElement("Load Selected AI Settings", textColor, null, null, delegate { LoadAISettingsInUI(); }, "IMPORTANT: Press this after changing the selected AI option. If you have selected to modify All AI then the Null Body settings will be loaded");
            //resetToDefaultElement = AISettingsMenuInterface.CreateFunctionElement("Load Default Values", textColor, null, null, delegate { Nothing(); }, "Press this if you load the selected AI's default values. Make sure you press apply afterwards");
            //enableRandomModeElement = AISettingsMenuInterface.CreateBoolElement("Random All Settings", textColor, false);
            configureAISettingsCategory2 = AISettingsMenuInterface.CreateCategoryElement("Configure Settings", textColor, ConfigureAISettings1, delegate { LoadAISettingsInUI(); }, "Open page 1 of the config menu for this AI. There are 9 pages");
            applySettingsToAIElement = AISettingsMenuInterface.CreateFunctionElement("Save Selected AI Settings", textColor, null, null, delegate { ApplyAISettings(selectedAIElement.GetValue()); }, "IMPORTANT: Press this after you have made any changes to the selected AI's configuration otherwise they will not be applied. You must press it for each AI's config that you change. E.g if you set a null body's settings, don't press apply, then switch the headcrab and change those settings, it will only save the headcrab settings not the null body's");

            //AI Settings RIP
            //Page 1
            aiHealthElement = ConfigureAISettings1.CreateFloatElement("Health", textColor, 0f, 100000f, 20f, 100f, 0, "HP", "AI health points. Default is 100");
            roamSpeedElement = ConfigureAISettings1.CreateFloatElement("Roam Speed", textColor, 0f, 1000f, 1f, 10f, 2, "","How fast the AI moves when walking around on their own");
            roamAngularSpeedElement = ConfigureAISettings1.CreateFloatElement("Roam Angular Speed", textColor, 0f, 10000f, 50f, 100f, 0, "", "Angular speed of the AI when walking around on their own");
            roamRangeXElement = ConfigureAISettings1.CreateFloatElement("Roam Range X", textColor, 0f, 100000f, 15f, 100f, 0, "", "How large an area the AI will freely roam in");
            roamRangeYElement = ConfigureAISettings1.CreateFloatElement("Roam Range Y", textColor, 0f, 100000f, 15f, 100f, 0, "", "How large an area the AI will freely roam in");
            nextPage3 = ConfigureAISettings1.CreateCategoryElement("Next Page", textColor, ConfigureAISettings2);
            //Page 2
            agroedSpeedElement = ConfigureAISettings2.CreateFloatElement("Agroed Speed", textColor, 0f, 100000f, 1f, 100f, 2, "","How fast the AI moves when attacking something");
            agroedAngularSpeedElement = ConfigureAISettings2.CreateFloatElement("Agroed Angular Speed", textColor, 0f, 10000f, 10f, 100f, 0, "","Angular speed of the AI when attacking something");
            investigationRangeElement = ConfigureAISettings2.CreateFloatElement("Investigation Range", textColor, 0f, 10000f, 20f, 100f, 0, "","Not totally sure. I'm guessing that it is how far the AI will go to investigate something?");
            breakAgroTargetDistanceElement = ConfigureAISettings2.CreateFloatElement("Break Agro On Target At Distance", textColor, 0f, 100000f, 15f, 50f, 0,"", "The distance that the AI has to be from something to stop attacking/chasing it");
            breakAgroHomeDistanceElement = ConfigureAISettings2.CreateFloatElement("Beak Agro When Distance From Home", textColor, 0f, 100000f, 15f, 50f, 0,"", "The distance that the AI has to be from home (where it spawned) to stop attacking/chasing something");
            nextPage4 = ConfigureAISettings2.CreateCategoryElement("Next Page", textColor, ConfigureAISettings3);
            //Page 3
            visionFOVElement = ConfigureAISettings3.CreateFloatElement("Vision FOV", textColor, 0f, 100000f, 10f, 85f, 0, "degrees",  "The FOV that the AI can see you within");
            hearingSensitivityElement = ConfigureAISettings3.CreateFloatElement("Hearing Sensitivity", textColor, 0f, 100000f, 10f, 0f, 0, "", "How sensitive the AI is to sounds");
            AdditionalMassElement = ConfigureAISettings3.CreateFloatElement("Additional Mass", textColor, 0f, 100000f, 50f, 0f, 0, "", "Add extra mass to the AI");
            crabletBaseColorElement = ConfigureAISettings3.CreateStringElement("Crablet Base Colour", textColor, colorOptions, "Default", "Colour of the crablet's ring when peaceful");
            crabletAgroedColorElement = ConfigureAISettings3.CreateStringElement("Crablet Agroed Colour", textColor, colorOptions, "Default", "Colour of the crablet's ring when attacking something");
            nextPage5 = ConfigureAISettings3.CreateCategoryElement("Next Page", textColor, ConfigureAISettings4);
            //Page 4
            aggressionElement = ConfigureAISettings4.CreateFloatElement("Aggression", textColor, 0f, 10f, 0.2f, 1f, 1, "", "How aggressive the AI is");
            irritabiltyElement = ConfigureAISettings4.CreateFloatElement("Irritability", textColor, 0f, 10f, 0.2f, 1f, 1, "", "How easily the AI gets irritated");
            placeabilityElement = ConfigureAISettings4.CreateFloatElement("Placeability", textColor, 0f, 10f, 0.2f, 1f, 1, "", "Not totally sure. I'm guessing that it is how easy the AI is to pick up?");
            vengefulnessElement = ConfigureAISettings4.CreateFloatElement("Vengefullness", textColor, 0f, 10f, 0.2f, 1f, 1, "", "How vengeful the AI is");
            nextPage6 = ConfigureAISettings4.CreateCategoryElement("Next Page", textColor, ConfigureAISettings5);
            //Page 5
            stunRecoveryTimeElement = ConfigureAISettings5.CreateFloatElement("Stun Recovery Time", textColor, 0f, 1000f, 0.5f, 1f, 1, "seconds", "How long the AI takes to recover after being stunned");
            maxStunTimeElement = ConfigureAISettings5.CreateFloatElement("Max Stun Time", textColor, 0f, 1000f, 1f, 1f, 0, "seconds", "Maximum time that the AI can be stunned for");
            minHeadImpactElement = ConfigureAISettings5.CreateFloatElement("Min Head Impact", textColor, 0f, 1000f, 10f, 1f, 0, "", "Minimum head impact required for the AI to take damage");
            minSpineImpactElement = ConfigureAISettings5.CreateFloatElement("Min Spine Impact", textColor, 0f, 1000f, 10f, 1f, 0, "", "Minimum spine impact required for the AI to take damage");
            minLimbImpactElement = ConfigureAISettings5.CreateFloatElement("Min Limb Impact", textColor, 0f, 1000f, 10f, 1f, 0, "", "Minimum limb impact required for the AI to take damage");
            nextPage7 = ConfigureAISettings5.CreateCategoryElement("Next Page", textColor, ConfigureAISettings6);
            //Page 6
            restingRangeElement = ConfigureAISettings6.CreateFloatElement("Resting Range", textColor, 0f, 1000f, 10f, 1f, 0, "", "Range that AI can see you within when resting");
            freezeWhileRestingElement = ConfigureAISettings6.CreateBoolElement("Freeze While Resting", textColor, false, "Whether or not the AI freezes when resting. Not sure what freezes really means though");
            homeIsPostElement = ConfigureAISettings6.CreateBoolElement("Home Is Post", textColor, false, "Not totally sure. Maybe means that AI will stay at their spawnpoint and not move even when agroed?");
            activeRangeElement = ConfigureAISettings6.CreateFloatElement("Active Range", textColor, 0f, 1000f, 10f, 1f, 0, "", "Range that AI can see you within when active");
            roamFrequencyElement = ConfigureAISettings6.CreateFloatElement("Roam Freqeuncy", textColor, 0f, 1f, 0.1f, 1f, 1, "", "Not totally sure. Probably how often the AI roam but the default values don't make much sense");
            nextPage8 = ConfigureAISettings6.CreateCategoryElement("Next Page", textColor, ConfigureAISettings7);
            //Page 7
            roamWanderElement = ConfigureAISettings7.CreateBoolElement("Roam Wander", textColor, false, "Whether or not the AI wander when roaming");
            enableThrowAttackElement = ConfigureAISettings7.CreateBoolElement("Enable Throw Attack", textColor, false, "Whether the AI's throw attack is enabled. Works on nullbodies and others");
            throwAttackMaxRangeElement = ConfigureAISettings7.CreateFloatElement("Throw Attack Max Range", textColor, 0f, 1000f, 10f, 1f, 0, "", "Maximum range that the throw zombies can throw");
            throwAttackMinRangeElement = ConfigureAISettings7.CreateFloatElement("Throw Attack Min Range", textColor, 0f, 1000f, 1f, 1f, 0, "", "Minimum range that the throw zombies can throw");
            throwCooldownElement = ConfigureAISettings7.CreateFloatElement("Throw Attack Cooldown", textColor, 0f, 1000f, 1f, 1f, 0, "seconds", "Cooldown for zombies to throw again");
            nextPage9 = ConfigureAISettings7.CreateCategoryElement("Next Page", textColor, ConfigureAISettings8);
            //Page 8
            throwVelocityElement = ConfigureAISettings8.CreateFloatElement("Throw Velocity", textColor, 0f, 1000f, 1f, 1f, 0, "", "Velocity of thrown attack from throwing zombies");
            gunRangeElement = ConfigureAISettings8.CreateFloatElement("Gun Range", textColor, 0f, 1000f, 20f, 1f, 0, "", "Range of Omni Projector's guns");
            gunCooldownElement = ConfigureAISettings8.CreateFloatElement("Gun Cooldown", textColor, 0f, 1000f, 0.5f, 1f, 1, "seconds", "Cooldown of Omni Projector's guns");
            gunAccuracyElement = ConfigureAISettings8.CreateFloatElement("Gun Accuracy", textColor, 0f, 1000f, 1f, 1f, 0, "", "Accuracy of Omni Projector's guns");
            reloadTimeElement = ConfigureAISettings8.CreateFloatElement("Reload Time", textColor, 0f, 1000f, 0.5f, 1f, 1, "seconds", "Time Omni Projector's take to reload");
            nextPage10 = ConfigureAISettings8.CreateCategoryElement("Next Page", textColor, ConfigureAISettings9);
            //Page 9
            clipSizeElement = ConfigureAISettings9.CreateIntElement("Clip Size", textColor, 0, 1000, 5, 1, "bullets", "Size of Omni Projector's clips");
            burstSizeElement = ConfigureAISettings9.CreateIntElement("Burst Size", textColor, 0, 1000, 5, 1, "bullets", "Amount of bullets fired in Omni Projector's burst shot");
            desiredGunDistanceElement = ConfigureAISettings9.CreateFloatElement("Desired Gun Distance", textColor, 0f, 1000f, 5f, 1f, 0, "", "Desired distance for Omni Projectors to shoot?");
            
            
        }
        /*
        void ApplyUIFont()
        {
            placeNewSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            findSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            foundSpawnerConfirmation.GetTextObject().GetComponent<TextMeshPro>().font = font;
            configureSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            selectedSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            saveSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            spawnerSettingsElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            aiSettingsElement.GetTextObject().GetComponent<TextMeshPro>().font = font; 
            setCustomSpawnAreaElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            setEnableAtDistanceElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            disableSpawnerAfterTimeCategoryElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            disableSpawnerAfterSpawnsCategoryElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            selectAItoSpawnCategory.GetTextObject().GetComponent<TextMeshPro>().font = font;
            setSpawnHeightElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            configureAISettingsCategory.GetTextObject().GetComponent<TextMeshPro>().font = font;
            deleteSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            visualiseSpawnHeightElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            spawnerSettingsNextPage.GetTextObject().GetComponent<TextMeshPro>().font = font;
            configureAIMovementCategory.GetTextObject().GetComponent<TextMeshPro>().font = font;
            setCustomWalkPoint.GetTextObject().GetComponent<TextMeshPro>().font = font;
            aiMovementOptionsElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage1.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage2.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage3.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage4.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage5.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage6.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage7.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage8.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage9.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nextPage10.GetTextObject().GetComponent<TextMeshPro>().font = font;
            configureAISettingsCategory2.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //spawn area
            //customSpawnArea.GetTextObject().GetComponent<TextMeshPro>().font = font;
            spawnAreaX.GetTextObject().GetComponent<TextMeshPro>().font = font;
            spawnAreaY.GetTextObject().GetComponent<TextMeshPro>().font = font;
            visualiseSpawnArea.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //spawn enable distance
            enableDistanceStatus.GetTextObject().GetComponent<TextMeshPro>().font = font;
            enableAtDistance.GetTextObject().GetComponent<TextMeshPro>().font = font;
            visualiseEnableDistance.GetTextObject().GetComponent<TextMeshPro>().font = font;

            spawnFrequencyElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            deadCleanUpFrequencyElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            maxConcurrentAliveElement.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //Remove spawner after time
            disableSpawnerAfterTimeEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            disableSpawnerAfterTimeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //Disable spawner after spawns
            disableSpawnerAfterSpawnsEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            disableSpawnerAfterSpawnsElement.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //AI Selection Interface
            randomAIEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nullbodyEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            fordearlyexitEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            omniturretEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nullbodycorruptedEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            fordearlyexitheadsetEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            fordvrJunkieEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            fordEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            omniwreckerEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            omniprojectorEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            crabletEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            crabletplusEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;
            nullratEnabled.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //AI Settings Menu
            selectedAIElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            //loadSelectedAISettingsElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            //resetToDefaultElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            //enableRandomModeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            applySettingsToAIElement.GetTextObject().GetComponent<TextMeshPro>().font = font;

            //AI Settings WTF
            aiHealthElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            roamSpeedElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            roamAngularSpeedElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            roamRangeXElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            roamRangeYElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            agroedSpeedElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            agroedAngularSpeedElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            investigationRangeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            breakAgroTargetDistanceElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            breakAgroHomeDistanceElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            visionFOVElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            hearingSensitivityElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            AdditionalMassElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            crabletBaseColorElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            crabletAgroedColorElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            aggressionElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            irritabiltyElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            placeabilityElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            vengefulnessElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            stunRecoveryTimeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            maxStunTimeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            minHeadImpactElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            minSpineImpactElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            minLimbImpactElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            restingRangeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            freezeWhileRestingElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            homeIsPostElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            activeRangeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            roamFrequencyElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            roamWanderElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            enableThrowAttackElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            throwAttackMaxRangeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            throwAttackMinRangeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            throwCooldownElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            throwVelocityElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            gunRangeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            gunCooldownElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            gunAccuracyElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            reloadTimeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            clipSizeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            burstSizeElement.GetTextObject().GetComponent<TextMeshPro>().font = font;
            desiredGunDistanceElement.GetTextObject().GetComponent<TextMeshPro>().font = font;

            appliedFont = true;
        }
        */

        void DeleteSelectedSpawner()
        {
            if (selectedSpawner != null)
            {
                //Physically remove and update others UI

                //remove selected spawner from active spawners list
                foreach (Spawner s in activeSpawners.ToList())
                {
                    if (s.spawnerNumber == selectedSpawner.spawnerNumber)
                    {
                        activeSpawners.Remove(s);
                    }
                }

                //remove selected spawner physical spawner
                foreach (GameObject gameObject in physicalActiveSpawners.ToList())
                {
                    if (gameObject.name == selectedSpawner.spawnPointID.ToString())
                    {
                        physicalActiveSpawners.Remove(gameObject);
                        GameObject.Destroy(gameObject);
                    }
                }

                //updating remaining spawner's names
                foreach (Spawner s in activeSpawners)
                {
                    //MelonModLogger.Log("The spawner number is " + s.spawnerNumber + " and the index is " + activeSpawners.IndexOf(s));
                    if (s.spawnerNumber != activeSpawners.IndexOf(s) + 1)
                    {
                        s.spawnerNumber = activeSpawners.IndexOf(s) + 1;
                        //MelonModLogger.Log("Set spawner number to " + s.spawnerNumber);
                    }
                }

                //MelonModLogger.Log("The active spawners list has count " + activeSpawners.Count());
                foreach (Spawner s in activeSpawners)
                {
                    //MelonModLogger.Log("Just iterated through a spawner");
                    UpdateSpawnerUI(s);
                }

                foundSpawnerConfirmation.SetText("No spawner selected");
                selectedSpawner = null;
            }
            
            
        }

        void PlaceSpawnerMode()
        {
            if (placeSpawnerMode == false)
            {
                //noSpawnerSelectedWarning = configureSpawnerInterface.CreateFunctionElement("No Spawner Selected", Color.red, null, null, delegate { Nothing(); });
                placeSpawnerMode = true;
                //mainMenu.CloseMenu();
                ghost.SetActive(true);
                placeSpawnerText = new GameObject("PlaceSpawnerText");
                TextMeshPro text = CreateText(placeSpawnerText, FontStyles.Bold, 0.5f, Color.black, new Color(0.4622f, 0.4622f, 0.4622f, 1f), 0.5f, TextAlignmentOptions.Center, "Close menu if you want to move. Press Left Trigger To Place Spawner", new Vector3(0, 0.07f, 0), new Vector4(9.6f, 9.6f, 9.6f, 9.6f));
                placeSpawnerText.transform.SetParent(player.transform, false);
                placeSpawnerText.transform.localPosition = new Vector3(0f, -0.05f, 1.5f);
            }
        }

        void SetWalkPointMode()
        {
            ApplySpawnerSettings(selectedSpawner);
            if(setWalkPointMode == false)
            {
                setWalkPointMode = true;
                ghost.SetActive(true);
                placeSpawnerText = new GameObject("PlaceSpawnerText");
                TextMeshPro text = CreateText(placeSpawnerText, FontStyles.Bold, 0.5f, Color.black, new Color(0.4622f, 0.4622f, 0.4622f, 1f), 0.5f, TextAlignmentOptions.Center, "Close menu if you want to move. Press Left Trigger To Set Walk Point", new Vector3(0, 0.07f, 0), new Vector4(9.6f, 9.6f, 9.6f, 9.6f));
                placeSpawnerText.transform.SetParent(player.transform, false);
                placeSpawnerText.transform.localPosition = new Vector3(0f, -0.05f, 1.5f);
            }
        }

        void FindSpawner()
        {
            foreach (Spawner spawner in activeSpawners)
            {
                if(player.transform.position.x > spawner.position.x - 2f && player.transform.position.x < spawner.position.x + 2f && player.transform.position.y > spawner.position.y - 2f && player.transform.position.y < spawner.position.y + 2f && spawner!=selectedSpawner)
                {
                    GameObject text = configureSpawnerElement.GetTextObject();
                    TextMeshPro tmp = text.GetComponent<TextMeshPro>();
                    tmp.text = "Configure Spawner";
                    //tmp.color = Color.green;

                    selectedSpawner = spawner;
                    /*
                    text = selectedSpawnerElement.GetTextObject();
                    tmp = text.GetComponent<TextMeshPro>();
                    tmp.text = "Selected Spawner " + selectedSpawner.spawnerNumber.ToString();
                    //tmp.color = Color.white;
                    */
                    text = foundSpawnerConfirmation.GetTextObject();
                    tmp = text.GetComponent<TextMeshPro>();
                    tmp.text = "Selected spawner " + selectedSpawner.spawnerNumber.ToString();
                    //tmp.color = Color.green;
                    break;
                }
            }
        }

        void Nothing() { }

        void LoadAISettingsInUI()
        {
            applySettingsToAIElement.GetTextObject().GetComponent<TextMeshPro>().text = "Save";
            applySettingsToAIElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.red;
           
            //int selectedAINumber = allAI.IndexOf(selectedAIElement.GetValue());
            if (selectedAIElement.GetValue() == "All")
            {
                AISettings thisAI = selectedSpawner.allAISettings.First();
                aiHealthElement.SetValue(thisAI.aiHealth);
                roamSpeedElement.SetValue(thisAI.roamSpeed);
                roamAngularSpeedElement.SetValue(thisAI.roamAngularSpeed);
                roamRangeXElement.SetValue(thisAI.roamRangeX);
                roamRangeYElement.SetValue(thisAI.roamRangeY);
                agroedSpeedElement.SetValue(thisAI.agroedSpeed);
                agroedAngularSpeedElement.SetValue(thisAI.agroedAngularSpeed);
                investigationRangeElement.SetValue(thisAI.investigationRange);
                breakAgroTargetDistanceElement.SetValue(thisAI.breakAgroTargetDistance);
                breakAgroHomeDistanceElement.SetValue(thisAI.breakAgroHomeDistance);
                visionFOVElement.SetValue(thisAI.visionFOV);
                hearingSensitivityElement.SetValue(thisAI.hearingSensitivity);
                AdditionalMassElement.SetValue(thisAI.additionalMass);
                crabletBaseColorElement.SetValue(thisAI.crabletBaseColor);
                crabletAgroedColorElement.SetValue(thisAI.crabletAgroedColor);
                aggressionElement.SetValue(thisAI.aggression);
                irritabiltyElement.SetValue(thisAI.irritability);
                placeabilityElement.SetValue(thisAI.placeability);
                vengefulnessElement.SetValue(thisAI.vengefulness);
                stunRecoveryTimeElement.SetValue(thisAI.stunRecoveryTime);
                maxStunTimeElement.SetValue(thisAI.maxStunTime);
                minHeadImpactElement.SetValue(thisAI.minHeadImpact);
                minSpineImpactElement.SetValue(thisAI.minSpineImpact);
                minLimbImpactElement.SetValue(thisAI.minLimbImpact);
                restingRangeElement.SetValue(thisAI.restingRange);
                freezeWhileRestingElement.SetValue(thisAI.freezeWhileResting);
                homeIsPostElement.SetValue(thisAI.homeIsPost);
                activeRangeElement.SetValue(thisAI.activeRange);
                roamFrequencyElement.SetValue(thisAI.roamFrequency);
                roamWanderElement.SetValue(thisAI.roamWander);
                enableThrowAttackElement.SetValue(thisAI.enableThrowAttack);
                throwAttackMaxRangeElement.SetValue(thisAI.throwAttackMaxRange);
                throwAttackMinRangeElement.SetValue(thisAI.throwAttackMinRange);
                throwCooldownElement.SetValue(thisAI.throwCooldown);
                throwVelocityElement.SetValue(thisAI.throwVelocity);
                gunRangeElement.SetValue(thisAI.gunRange);
                gunCooldownElement.SetValue(thisAI.gunCooldown);
                gunAccuracyElement.SetValue(thisAI.gunAccuracy);
                reloadTimeElement.SetValue(thisAI.reloadTime);
                clipSizeElement.SetValue(thisAI.clipSize);
                burstSizeElement.SetValue(thisAI.burstSize);
                desiredGunDistanceElement.SetValue(thisAI.desiredGunDistance);
            }
            else
            {
                AISettings thisAI = selectedSpawner.allAISettings.First();

                foreach (AISettings ai in selectedSpawner.allAISettings)
                {
                    if (ai.AI == selectedAIElement.GetValue())
                    {
                        thisAI = ai;
                        break;
                    }
                }

                aiHealthElement.SetValue(thisAI.aiHealth);
                roamSpeedElement.SetValue(thisAI.roamSpeed);
                roamAngularSpeedElement.SetValue(thisAI.roamAngularSpeed);
                roamRangeXElement.SetValue(thisAI.roamRangeX);
                roamRangeYElement.SetValue(thisAI.roamRangeY);
                agroedSpeedElement.SetValue(thisAI.agroedSpeed);
                agroedAngularSpeedElement.SetValue(thisAI.agroedAngularSpeed);
                investigationRangeElement.SetValue(thisAI.investigationRange);
                breakAgroTargetDistanceElement.SetValue(thisAI.breakAgroTargetDistance);
                breakAgroHomeDistanceElement.SetValue(thisAI.breakAgroHomeDistance);
                visionFOVElement.SetValue(thisAI.visionFOV);
                hearingSensitivityElement.SetValue(thisAI.hearingSensitivity);
                AdditionalMassElement.SetValue(thisAI.additionalMass);
                crabletBaseColorElement.SetValue(thisAI.crabletBaseColor);
                crabletAgroedColorElement.SetValue(thisAI.crabletAgroedColor);
                aggressionElement.SetValue(thisAI.aggression);
                irritabiltyElement.SetValue(thisAI.irritability);
                placeabilityElement.SetValue(thisAI.placeability);
                vengefulnessElement.SetValue(thisAI.vengefulness);
                stunRecoveryTimeElement.SetValue(thisAI.stunRecoveryTime);
                maxStunTimeElement.SetValue(thisAI.maxStunTime);
                minHeadImpactElement.SetValue(thisAI.minHeadImpact);
                minSpineImpactElement.SetValue(thisAI.minSpineImpact);
                minLimbImpactElement.SetValue(thisAI.minLimbImpact);
                restingRangeElement.SetValue(thisAI.restingRange);
                freezeWhileRestingElement.SetValue(thisAI.freezeWhileResting);
                homeIsPostElement.SetValue(thisAI.homeIsPost);
                activeRangeElement.SetValue(thisAI.activeRange);
                roamFrequencyElement.SetValue(thisAI.roamFrequency);
                roamWanderElement.SetValue(thisAI.roamWander);
                enableThrowAttackElement.SetValue(thisAI.enableThrowAttack);
                throwAttackMaxRangeElement.SetValue(thisAI.throwAttackMaxRange);
                throwAttackMinRangeElement.SetValue(thisAI.throwAttackMinRange);
                throwCooldownElement.SetValue(thisAI.throwCooldown);
                throwVelocityElement.SetValue(thisAI.throwVelocity);
                gunRangeElement.SetValue(thisAI.gunRange);
                gunCooldownElement.SetValue(thisAI.gunCooldown);
                gunAccuracyElement.SetValue(thisAI.gunAccuracy);
                reloadTimeElement.SetValue(thisAI.reloadTime);
                clipSizeElement.SetValue(thisAI.clipSize);
                burstSizeElement.SetValue(thisAI.burstSize);
                desiredGunDistanceElement.SetValue(thisAI.desiredGunDistance);
            }


            



            /*
            switch (selectedAIElement.GetValue())
            {
                case "Null Body":
                    selectedAINumber = 0;
                    break;
                case "Ford Early Exit":
                    selectedAINumber = 1;
                    break;
                case "Omniturret":
                    selectedAINumber = 2;
                    break;
                case "Null Body Corrupted":
                    selectedAINumber = 3;
                    break;
                case "Ford Early Exit Headset":
                    selectedAINumber = 4;
                    break;
                case "Ford VR Junkie":
                    selectedAINumber = 5;
                    break;
                case "Ford":
                    selectedAINumber = 6;
                    break;
                case "Omni Wrecker":
                    selectedAINumber = 7;
                    break;
                case "Omni Projector":
                    selectedAINumber = 8;
                    break;
                case "Crablet":
                    selectedAINumber = 9;
                    break;
                case "Crablet Plus":
                    selectedAINumber = 10;
                    break;
                case "Null Rat":
                    selectedAINumber = 0;
                    break;
            }
            */
        }

        void LoadSpawnerSettingsInUI()
        {
            if(selectedSpawner != null)
            {
                //MelonModLogger.Log("THE SELECTED SPAWNER IS NOT NULL");
                //Updating selected AI UI options

                if (selectedSpawner.spawningActive)
                {
                    toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Spawner Active";
                    toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.green;
                }
                else
                {
                    toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Spawner Inactive";
                    toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.red;
                }

                saveSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Save";
                saveSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.red;

                //resetting all selected ai values 
                randomAIEnabled.SetValue(true);
                nullbodyEnabled.SetValue(false);
                fordearlyexitEnabled.SetValue(false);
                omniturretEnabled.SetValue(false);
                nullbodycorruptedEnabled.SetValue(false);
                fordearlyexitheadsetEnabled.SetValue(false);
                fordvrJunkieEnabled.SetValue(false);
                fordEnabled.SetValue(false);
                omniwreckerEnabled.SetValue(false);
                omniprojectorEnabled.SetValue(false);
                crabletEnabled.SetValue(false);
                crabletplusEnabled.SetValue(false);
                nullratEnabled.SetValue(false);


                randomAIEnabled.SetValue(selectedSpawner.randomEnemy);
                foreach(int i in selectedSpawner.enabledEnemies)
                {
                    switch (i) 
                    {
                        case 0:
                            nullbodyEnabled.SetValue(true);
                            break;
                        case 1:
                            fordearlyexitEnabled.SetValue(true);
                            break;
                        case 2:
                            omniturretEnabled.SetValue(true);
                            break;
                        case 3:
                            nullbodycorruptedEnabled.SetValue(true);
                            break;
                        case 4:
                            fordearlyexitheadsetEnabled.SetValue(true);
                            break;
                        case 5:
                            fordvrJunkieEnabled.SetValue(true);
                            break;
                        case 6:
                            fordEnabled.SetValue(true);
                            break;
                        case 7:
                            omniwreckerEnabled.SetValue(true);
                            break;
                        case 8:
                            omniprojectorEnabled.SetValue(true);
                            break;
                        case 9:
                            crabletEnabled.SetValue(true);
                            break;
                        case 10:
                            crabletplusEnabled.SetValue(true);
                            break;
                        case 11:
                            nullratEnabled.SetValue(true);
                            break;
                    }
                }

                //Updating spawner settings
                //selectedSpawnerElement.SetText("Selected Spawner " + selectedSpawner.spawnerNumber);
                //selectedSpawnerElement.SetColor(textColor);
                selectedSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Selected Spawner " + selectedSpawner.spawnerNumber;
                selectedSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = textColor;

                //customSpawnArea.SetValue(selectedSpawner.useSpawnArea);
                spawnAreaX.SetValue(selectedSpawner.spawnAreaSize.x);
                spawnHeightElement.SetValue(selectedSpawner.maxHeight);
                spawnAreaY.SetValue(selectedSpawner.spawnAreaSize.y);
                showSpawnerUIElement.SetValue(selectedSpawner.UIEnabled);
                enableDistanceStatus.SetValue(selectedSpawner.useDistanceActivation);
                enableAtDistance.SetValue(selectedSpawner.distanceToActivate);
                disableSpawnerAfterTimeEnabled.SetValue(selectedSpawner.useSpawnTimeLimit);
                disableSpawnerAfterTimeElement.SetValue(selectedSpawner.spawnTimeLimit);
                disableSpawnerAfterSpawnsEnabled.SetValue(selectedSpawner.useSpawnLimit);
                disableSpawnerAfterSpawnsElement.SetValue(selectedSpawner.spawnLimit);
                maxConcurrentAliveElement.SetValue(selectedSpawner.maxenemyCount);
                spawnFrequencyElement.SetValue(selectedSpawner.spawnFrequency);
                deadCleanUpFrequencyElement.SetValue(selectedSpawner.cleardeadFrequency);
                disableSpawnerAfterSpawnsEnabled.SetValue(selectedSpawner.useSpawnLimit);
                disableSpawnerAfterSpawnsElement.SetValue(selectedSpawner.spawnLimit);
                aiMovementOptionsElement.SetValue(selectedSpawner.movementMode);

            }
            else
            {
                selectedSpawnerElement.SetText("NO SPAWNER SELECTED");
                selectedSpawnerElement.SetColor(Color.red);
            }
        }

        void ApplyAISettings(string selectedAI)
        {
            applySettingsToAIElement.GetTextObject().GetComponent<TextMeshPro>().text = "Save";
            applySettingsToAIElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.green;

            bool changeAll = false;
            int selectedAINumber=0;
            switch (selectedAI)
            {
                case "Null Body":
                    selectedAINumber = 0;
                    break;
                case "Ford Early Exit":
                    selectedAINumber = 1;
                    break;
                case "Omniturret":
                    selectedAINumber = 2;
                    break;
                case "Null Body Corrupted":
                    selectedAINumber = 3;
                    break;
                case "Ford Early Exit Headset":
                    selectedAINumber = 4;
                    break;
                case "Ford VR Junkie":
                    selectedAINumber = 5;
                    break;
                case "Ford":
                    selectedAINumber = 6;
                    break;
                case "Omni Wrecker":
                    selectedAINumber = 7;
                    break;
                case "Omni Projector":
                    selectedAINumber = 8;
                    break;
                case "Crablet":
                    selectedAINumber = 9;
                    break;
                case "Crablet Plus":
                    selectedAINumber = 10;
                    break;
                case "Null Rat":
                    selectedAINumber = 11;
                    break;
                case "All":
                    changeAll = true;
                    break;
            }

            if (changeAll)
            {
                for(int i = 0;i<11;i++)
                {
                    selectedSpawner.allAISettings[i].aiHealth = aiHealthElement.GetValue();
                    selectedSpawner.allAISettings[i].roamSpeed = roamSpeedElement.GetValue();
                    selectedSpawner.allAISettings[i].roamAngularSpeed = roamAngularSpeedElement.GetValue();
                    selectedSpawner.allAISettings[i].roamRangeX = roamRangeXElement.GetValue();
                    selectedSpawner.allAISettings[i].roamRangeY = roamRangeYElement.GetValue();
                    selectedSpawner.allAISettings[i].agroedSpeed = agroedSpeedElement.GetValue();
                    selectedSpawner.allAISettings[i].agroedAngularSpeed = agroedAngularSpeedElement.GetValue();
                    selectedSpawner.allAISettings[i].investigationRange = investigationRangeElement.GetValue();
                    selectedSpawner.allAISettings[i].breakAgroTargetDistance = breakAgroTargetDistanceElement.GetValue();
                    selectedSpawner.allAISettings[i].breakAgroHomeDistance = breakAgroHomeDistanceElement.GetValue();
                    selectedSpawner.allAISettings[i].visionFOV = visionFOVElement.GetValue();
                    selectedSpawner.allAISettings[i].hearingSensitivity = hearingSensitivityElement.GetValue();
                    selectedSpawner.allAISettings[i].additionalMass = AdditionalMassElement.GetValue();
                    selectedSpawner.allAISettings[i].crabletBaseColor = crabletBaseColorElement.GetValue();
                    selectedSpawner.allAISettings[i].crabletAgroedColor = crabletAgroedColorElement.GetValue();
                    selectedSpawner.allAISettings[i].aggression = aggressionElement.GetValue();
                    selectedSpawner.allAISettings[i].irritability = irritabiltyElement.GetValue();
                    selectedSpawner.allAISettings[i].placeability = placeabilityElement.GetValue();
                    selectedSpawner.allAISettings[i].vengefulness = vengefulnessElement.GetValue();
                    selectedSpawner.allAISettings[i].stunRecoveryTime = stunRecoveryTimeElement.GetValue();
                    selectedSpawner.allAISettings[i].maxStunTime = maxStunTimeElement.GetValue();
                    selectedSpawner.allAISettings[i].minHeadImpact = minHeadImpactElement.GetValue();
                    selectedSpawner.allAISettings[i].minSpineImpact = minSpineImpactElement.GetValue();
                    selectedSpawner.allAISettings[i].minLimbImpact = minLimbImpactElement.GetValue();
                    selectedSpawner.allAISettings[i].restingRange = restingRangeElement.GetValue();
                    selectedSpawner.allAISettings[i].freezeWhileResting = freezeWhileRestingElement.GetValue();
                    selectedSpawner.allAISettings[i].homeIsPost = homeIsPostElement.GetValue();
                    selectedSpawner.allAISettings[i].activeRange = activeRangeElement.GetValue();
                    selectedSpawner.allAISettings[i].roamFrequency = roamFrequencyElement.GetValue();
                    selectedSpawner.allAISettings[i].roamWander = roamWanderElement.GetValue();
                    selectedSpawner.allAISettings[i].enableThrowAttack = enableThrowAttackElement.GetValue();
                    selectedSpawner.allAISettings[i].throwAttackMaxRange = throwAttackMaxRangeElement.GetValue();
                    selectedSpawner.allAISettings[i].throwAttackMinRange = throwAttackMinRangeElement.GetValue();
                    selectedSpawner.allAISettings[i].throwCooldown = throwCooldownElement.GetValue();
                    selectedSpawner.allAISettings[i].throwVelocity = throwVelocityElement.GetValue();
                    selectedSpawner.allAISettings[i].gunRange = gunRangeElement.GetValue();
                    selectedSpawner.allAISettings[i].gunCooldown = gunCooldownElement.GetValue();
                    selectedSpawner.allAISettings[i].gunAccuracy = gunAccuracyElement.GetValue();
                    selectedSpawner.allAISettings[i].reloadTime = reloadTimeElement.GetValue();
                    selectedSpawner.allAISettings[i].clipSize = clipSizeElement.GetValue();
                    selectedSpawner.allAISettings[i].burstSize = burstSizeElement.GetValue();
                    selectedSpawner.allAISettings[i].desiredGunDistance = desiredGunDistanceElement.GetValue();
                }
            }

            selectedSpawner.allAISettings[selectedAINumber].aiHealth = aiHealthElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].roamSpeed= roamSpeedElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].roamAngularSpeed= roamAngularSpeedElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].roamRangeX= roamRangeXElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].roamRangeY= roamRangeYElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].agroedSpeed= agroedSpeedElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].agroedAngularSpeed= agroedAngularSpeedElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].investigationRange= investigationRangeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].breakAgroTargetDistance= breakAgroTargetDistanceElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].breakAgroHomeDistance= breakAgroHomeDistanceElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].visionFOV= visionFOVElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].hearingSensitivity= hearingSensitivityElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].additionalMass= AdditionalMassElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].crabletBaseColor = crabletBaseColorElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].crabletAgroedColor = crabletAgroedColorElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].aggression= aggressionElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].irritability= irritabiltyElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].placeability= placeabilityElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].vengefulness= vengefulnessElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].stunRecoveryTime= stunRecoveryTimeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].maxStunTime= maxStunTimeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].minHeadImpact= minHeadImpactElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].minSpineImpact= minSpineImpactElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].minLimbImpact= minLimbImpactElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].restingRange= restingRangeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].freezeWhileResting= freezeWhileRestingElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].homeIsPost= homeIsPostElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].activeRange= activeRangeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].roamFrequency= roamFrequencyElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].roamWander= roamWanderElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].enableThrowAttack= enableThrowAttackElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].throwAttackMaxRange= throwAttackMaxRangeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].throwAttackMinRange= throwAttackMinRangeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].throwCooldown= throwCooldownElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].throwVelocity= throwVelocityElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].gunRange= gunRangeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].gunCooldown= gunCooldownElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].gunAccuracy= gunAccuracyElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].reloadTime= reloadTimeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].clipSize= clipSizeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].burstSize= burstSizeElement.GetValue();
            selectedSpawner.allAISettings[selectedAINumber].desiredGunDistance= desiredGunDistanceElement.GetValue();


        }

        void ToggleSpawnPointActivation(Spawner spawnPoint)
        {
            spawnPoint.spawningActive = !spawnPoint.spawningActive;
            if (selectedSpawner.spawningActive)
            {
                toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Spawner Active";
                toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.green;
            }
            else
            {
                toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Spawner Inactive";
                toggleSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.red;
            }
            ApplySpawnerSettings(spawnPoint);
            UpdateSpawnerUI(spawnPoint);
        }

        void SaveSpawnerSettings(Spawner spawnPoint)
        {
            if(spawnPoint != null)
            {
                ApplySpawnerSettings(spawnPoint);
                UpdateSpawnerUI(spawnPoint);
                saveSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().text = "Save";
                saveSpawnerElement.GetTextObject().GetComponent<TextMeshPro>().color = Color.green;
            }
        }
        
        void GetFont()
        {
            if (GameObject.Find("CANVAS_OPTIONS") != null)
            {
                GameObject canvas = GameObject.Find("CANVAS_OPTIONS");
                TextMeshPro[] textmeshpro = canvas.GetComponentsInChildren<TextMeshPro>();
                foreach (var t in textmeshpro)
                {
                    if (t.font != null)
                    {
                        gotFont = true;
                        font = t.font;
                        break;
                    }
                }
            }
        }

        void GetHands()
        {
            Hand[] array = UnityEngine.Object.FindObjectsOfType<Hand>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].name.ToLower().Contains("hand"))
                {
                    if (array[i].name.ToLower().Contains("left"))
                    {
                        leftHand = array[i];
                    }
                    else if (array[i].name.ToLower().Contains("right"))
                    {
                        rightHand = array[i];
                    }
                }
            }
        }

        static TextMeshPro CreateText(GameObject component, FontStyles fontStyles, float outLineWidth, Color outlineColor, Color textColor, float fontSize, TextAlignmentOptions textAlignment, String text, Vector3 localPosition, Vector4 margin)
        {
            TextMeshPro textMeshPro = new TextMeshPro();
            textMeshPro = component.AddComponent<TextMeshPro>();
            textMeshPro.font = font;
            textMeshPro.fontStyle = fontStyles;
            textMeshPro.outlineWidth = outLineWidth;
            textMeshPro.outlineColor = outlineColor;
            textMeshPro.color = textColor;
            textMeshPro.fontSize = fontSize;//normal is 0.3
            textMeshPro.alignment = textAlignment;
            textMeshPro.margin = margin;
            textMeshPro.text = text;
            textMeshPro.transform.localPosition = localPosition;
            return textMeshPro;
        }
    }
}

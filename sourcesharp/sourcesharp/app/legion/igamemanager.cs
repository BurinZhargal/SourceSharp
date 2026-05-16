using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sourcesharp.app.legion
{
    public abstract class IGameManager
    {
        // GameManagers are expected to implement these methods.
        public abstract bool Init();
        public abstract LevelRetVal_t LevelInit(bool bFirstCall);
        public abstract void Update();
        public abstract LevelRetVal_t LevelShutdown(bool bFirstCall);
        public abstract void Shutdown();

        // Called during game save
        public abstract void OnSave();

        // Called during game restore
        public abstract void OnRestore();

        // Is this game manager involved in I/O or simulation?
        public abstract bool PerformsSimulation();

        // Add, remove game managers
        public static void Add(IGameManager pSys);
        public static void Remove(IGameManager pSys);
        public static void RemoveAll();

        // Init, shutdown game managers
        public static bool InitAllManagers();
        public static void ShutdownAllManagers();

        // Start, stop running game managers
        public static void Start();
        public static void Stop();
        public static int FrameNumber();

        // Used in simulation
        public static float CurrentSimulationTime();
        public static float SimulationDeltaTime();

        // Used in rendering
        public static float CurrentTime();
        public static float DeltaTime();

        // Start loading a level
        public static void StartNewLevel();
        public static void ShutdownLevel();
        public static LevelState_t GetLevelState();

        protected IGameManager() { }

        protected delegate LevelRetVal_t GameManagerLevelFunc_t(bool bFirstCall);
        protected delegate bool GameManagerInitFunc_t();
        protected delegate void GameManagerFunc_t();

        // Used to invoke a method of all added game managers in order
        protected static void InvokeMethod(GameManagerFunc_t f);
        protected static void InvokeMethodReverseOrder(GameManagerFunc_t f);
        protected static bool InvokeMethod(GameManagerInitFunc_t f);
        protected static LevelRetVal_t InvokeLevelMethod(GameManagerLevelFunc_t f, bool bFirstCall);
        protected static LevelRetVal_t InvokeLevelMethodReverseOrder(GameManagerLevelFunc_t f, bool bFirstCall);

        protected static bool m_bLevelShutdownRequested;
        protected static bool m_bLevelStartRequested;
        protected static bool m_bStopRequested;
        protected static List<IGameManager> m_GameManagers;
        protected static bool m_bIsRunning;
        protected static bool m_bIsInitialized;
        protected static int m_nFrameNumber;
        protected static float m_flCurrentTime;
        protected static float m_flLastTime;
        static LevelState_t m_LevelState;
   
}

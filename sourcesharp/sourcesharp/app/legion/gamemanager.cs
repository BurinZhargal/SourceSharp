using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sourcesharp.app.legion
{ 

    public enum LevelState_t
    {
        NOT_IN_LEVEL,
        LOADING_LEVEL,
        IN_LEVEL,
        SHUTTING_DOWN_LEVEL
    }

    public enum LevelRetVal_t
    {
        FAILED,
        MORE_WORK,
        FINISHED
    }


    public class GameManager
    {
        private bool m_bLevelShutdownRequested;
        private bool m_bLevelStartRequested;
        private LevelState_t m_LevelState;

        private const float TICK_INTERVAL = 0.015f;

        public void StartNewLevel()
        {
            m_bLevelShutdownRequested = true;
            m_bLevelStartRequested = true;
        }

        public void ShutdownLevel()
        {
            m_bLevelShutdownRequested = true;
        }

        // Additional properties, fields, and methods can be added here as needed
    }


    public class GameManager : IGameManager
    {
        public static int m_nFrameNumber = 0;
        public static bool m_bStopRequested = false;
        public static bool m_bIsRunning = false;
        public static bool m_bIsInitialized = false;
        public static bool m_bLevelStartRequested = false;
        public static bool m_bLevelShutdownRequested = false;
        public static float m_flCurrentTime = 0.0f;
        public static float m_flLastTime = 0.0f;
        public static LevelState_t m_LevelState = LevelState_t.NOT_IN_LEVEL;

        public static List<IGameManager> m_GameManagers = new List<IGameManager>();

        public override void Init()
        {
            m_bIsInitialized = true;
        }

        public override void Shutdown()
        {

        }

        public override void RunFrame()
        {
            m_nFrameNumber++;
            float currentTime = GetCurrentTime();
            float deltaTime = currentTime - m_flLastTime;
            m_flLastTime = currentTime;

            // FIXME: Sleep( 1 ) ??

            RunFrameInternal(deltaTime);
        }

        void RunFrameInternal(float deltaTime)
        {
            // Handle level start/shutdown
            if (m_bLevelStartRequested)
            {
                MyLevelInitPreEntity();
                MyLevelInitPostEntity();
                MyLevelStarted();
                m_LevelState = IN_LEVEL;
                m_bLevelStartRequested = false;
            }
            else if (m_bLevelShutdownRequested)
            {
                m_LevelState = NOT_IN_LEVEL;
                MyLevelShutdown();
                m_bLevelShutdownRequested = false;
            }

            // Run everything else
            for (int i = 0; i < m_GameManagers.Count; ++i)
            {
                m_GameManagers[i].Update(deltaTime);
            }
        }

        void Update(float deltaTime)
        {

        }

        void MyLevelInitPreEntity()
        {

        }

        void MyLevelInitPostEntity()
        {

        }

        void MyLevelShutdown()
        {

        }

        void MyLevelStarted()
        {

        }

        float GetCurrentTime()
        {
            return (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        }
        void Remove(IGameManager* pSys)
        {
            Assert(!m_bIsRunning);
            m_GameManagers.FindAndRemove(pSys);
        }

        void RemoveAll()
        {
            m_GameManagers.RemoveAll();
        }

        void InvokeMethod(GameManagerFunc_t f)
        {
            int i;
            int nCount = m_GameManagers.Count();
            for (i = 0; i < nCount; ++i)
            {
                (m_GameManagers[i]->* f)();
            }
        }

        void InvokeMethodReverseOrder(GameManagerFunc_t f)
        {
            int i;
            int nCount = m_GameManagers.Count();
            for (i = nCount; --i >= 0;)
            {
                (m_GameManagers[i]->* f)();
            }
        }
        public bool InvokeMethod(GameManagerInitFunc_t f)
        {
            int nCount = m_GameManagers.Count();
            for (int i = 0; i < nCount; i++)
            {
                if (!(m_GameManagers[i].* f)())
                    return false;
            }
            return true;
        }

        public LevelRetVal_t InvokeLevelMethod(GameManagerLevelFunc_t f, bool bFirstCall)
        {
            LevelRetVal_t nRetVal = LevelRetVal_t.FINISHED;
            int nCount = m_GameManagers.Count();
            for (int i = 0; i < nCount; i++)
            {
                LevelRetVal_t val = (m_GameManagers[i].* f)(bFirstCall);
                if (val == LevelRetVal_t.FAILED)
                    return LevelRetVal_t.FAILED;
                if (val == LevelRetVal_t.MORE_WORK)
                    nRetVal = LevelRetVal_t.MORE_WORK;
            }
            return nRetVal;
        }

        LevelRetVal_t IGameManager::InvokeLevelMethodReverseOrder(GameManagerLevelFunc_t f, bool bFirstCall)
        {
            LevelRetVal_t nRetVal = FINISHED;
            int i;
            int nCount = m_GameManagers.Count();
            for (i = 0; i < nCount; ++i)
            {
                LevelRetVal_t val = (m_GameManagers[i]->* f)(bFirstCall);
                if (val == FAILED)
                {
                    nRetVal = FAILED;
                }
                if ((val == MORE_WORK) && (nRetVal != FAILED))
                {
                    nRetVal = MORE_WORK;
                }
            }
            return nRetVal;
        }
        public bool InitAllManagers()
        {
            m_nFrameNumber = 0;
            if (InvokeMethod(new GameManagerFunc_t(IGameManager.Init)))
            {
                m_bIsInitialized = true;
                return true;
            }

            return false;
        }

        public void ShutdownAllManagers()
        {
            if (m_bIsInitialized)
            {
                InvokeMethodReverseOrder(new GameManagerFunc_t(IGameManager.Shutdown));
                m_bIsInitialized = false;
            }
        }
        void UpdateLevelStateMachine()
        {
            // Do we want to switch into the level shutdown state?
            var bFirstLevelShutdownFrame = false;
            if (m_bLevelShutdownRequested)
            {
                if (m_LevelState != LOADING_LEVEL)
                {
                    m_bLevelShutdownRequested = false;
                }

                if (m_LevelState == IN_LEVEL)
                {
                    m_LevelState = SHUTTING_DOWN_LEVEL;
                    bFirstLevelShutdownFrame = true;
                }
            }

            // Perform level shutdown
            if (m_LevelState == SHUTTING_DOWN_LEVEL)
            {
                var val = InvokeLevelMethodReverseOrder(LevelShutdown, bFirstLevelShutdownFrame);
                if (val != MORE_WORK)
                {
                    m_LevelState = NOT_IN_LEVEL;
                }
            }

            // Do we want to switch into the level startup state?
            var bFirstLevelStartFrame = false;
            if (m_bLevelStartRequested)
            {
                if (m_LevelState != SHUTTING_DOWN_LEVEL)
                {
                    m_bLevelStartRequested = false;
                }

                if (m_LevelState == NOT_IN_LEVEL)
                {
                    m_LevelState = LOADING_LEVEL;
                    bFirstLevelStartFrame = true;
                }
            }

            // Perform level load
            if (m_LevelState == LOADING_LEVEL)
            {
                var val = InvokeLevelMethod(LevelInit, bFirstLevelStartFrame);
                if (val == LevelRetVal_t.FAILED)
                {
                    m_LevelState = NOT_IN_LEVEL;
                }
                else if (val == LevelRetVal_t.FINISHED)
                {
                    m_LevelState = IN_LEVEL;
                }
            }
        }
        public void Start()
        {
            Debug.Assert(!m_bIsRunning && m_bIsInitialized);

            m_bIsRunning = true;
            m_bStopRequested = false;

            // This option is useful when running the app twice on the same machine
            // It makes the 2nd instance of the app run a lot faster
            bool bPlayNice = (CommandLine().CheckParm("-yieldcycles") != 0);

            float flStartTime = m_flCurrentTime = m_flLastTime = TimeUtils.Plat_FloatTime();
            int nFramesSimulated = 0;
            int nCount = m_GameManagers.Count;
            while (!m_bStopRequested)
            {
                UpdateLevelStateMachine();

                m_flLastTime = m_flCurrentTime;
                m_flCurrentTime = TimeUtils.Plat_FloatTime();
                int nSimulationFramesNeeded = 1 + (int)((m_flCurrentTime - flStartTime) / TICK_INTERVAL);
                while (nSimulationFramesNeeded > nFramesSimulated)
                {
                    for (int i = 0; i < nCount; ++i)
                    {
                        if (m_GameManagers[i].PerformsSimulation())
                        {
                            m_GameManagers[i].Update();
                        }
                    }
                    ++m_nFrameNumber;
                    ++nFramesSimulated;
                }

                // Always do I/O related managers regardless of framerate
                for (int i = 0; i < nCount; ++i)
                {
                    if (!m_GameManagers[i].PerformsSimulation())
                    {
                        m_GameManagers[i].Update();
                    }
                }

                if (bPlayNice)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }

            m_bIsRunning = false;
        }
        public void StartNewLevel()
        {
            m_bLevelShutdownRequested = true;
            m_bLevelStartRequested = true;
        }

        public void ShutdownLevel()
        {
            m_bLevelShutdownRequested = true;
        }
        public class CGameManager<BaseClass> : IGameManager
        {
            public virtual bool Init()
            {
                return true;
            }

            public virtual LevelRetVal_t LevelInit(bool bFirstCall)
            {
                return LevelRetVal_t.FINISHED;
            }

            public virtual void Update()
            {
            }

            public virtual LevelRetVal_t LevelShutdown(bool bFirstCall)
            {
                return LevelRetVal_t.FINISHED;
            }

            public virtual void Shutdown()
            {
            }

            public virtual void OnSave()
            {
            }

            public virtual void OnRestore()
            {
            }

            public virtual bool PerformsSimulation()
            {
                return false;
            }
        }
        public void Dispose()
        {
            Remove(this);
        }







    }

}

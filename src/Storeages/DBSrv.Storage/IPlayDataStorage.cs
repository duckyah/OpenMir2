using DBSrv.Storage.Model;
using System.Collections.Specialized;
using SystemModule.Packets.ServerPackets;

namespace DBSrv.Storage
{
    /// <summary>
    /// 角色数据存储接口
    /// </summary>
    public interface IPlayDataStorage
    {
        void LoadQuickList();

        int Index(string sName);

        int Get(int nIndex, ref PlayerDataInfo HumanRCD);

        bool Get(string chrName, ref PlayerDataInfo HumanRCD);

        PlayerInfoData Query(int playerId);

        bool GetQryChar(int nIndex, ref QueryChr QueryChrRcd);

        bool Update(string chrName, PlayerDataInfo HumanRCD);

        bool UpdateQryChar(int nIndex, QueryChr QueryChrRcd);

        bool Add(PlayerDataInfo HumanRCD);

        int Find(string sChrName, StringDictionary List);

        bool Delete(int nIndex);

        bool Delete(string sChrName);

        int Count();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    public static class PartitioningHelper
    {
        /// <summary>
        /// Returns the partitionId for the string given the partition count
        /// A Md5 hash is use to turn the string bytes into a virtual partition
        /// then a modulo get it back into the expected range.
        /// </summary>
        public static int GetPartitionId(this string partitionKey, int partitionCount, Encoding encoding = null)
        {
            var bytes = partitionKey.ToMd5Bytes(encoding);
            return GetPartitionId(bytes, partitionCount);
        }

        /// <summary>
        /// Returns the partitionId for the guid given the partition count
        /// </summary>
        public static int GetPartitionId(this Guid id, int partitionCount)
        {
            var bytes = id.ToByteArray();
            return GetPartitionId(bytes, partitionCount);
        }

        /// <summary>
        /// Returns the partitionId for the bytes (as a big integer) given the partition count
        /// </summary>
        public static int GetPartitionId(this byte[] bytes, int partitionCount)
        {
            var bigInt = new BigInteger(bytes);
            var modulus = bigInt % partitionCount;
            return Math.Abs((int)modulus);
        }

        public static int GetPartitionId(this int id, int partitionCount) => Math.Abs(id % partitionCount);

        public static int GetPartitionId(this long id, int partitionCount) => (int)Math.Abs(id % (long)partitionCount);


        internal static int GetPartitionId_MOD_ON_TOP_BYTES(Guid id, int partitionCount)
        { // give best results 

            var bytes = id.ToByteArray();
            var uint64 = BitConverter.ToUInt64(bytes, 0);
            return (int)(uint64 % (ulong)partitionCount);

            //Min: 25
            //Max: 79
        }

        internal static int GetPartitionId_MOD_ON_LAST_BYTES(Guid id, int partitionCount)
        { // give best results 

            var bytes = id.ToByteArray();
            var uint64 = BitConverter.ToUInt64(bytes, 8);
            return Math.Abs((int)(uint64 % (ulong)partitionCount));

            //Min: 22
            //Max: 77
        }

        internal static int GetPartitionId_CRAPPY(Guid id, int partitionCount)
        {
            // crappy results  => distribution triangulaire : https://fr.wikipedia.org/wiki/Probabilit%C3%A9s_des_d%C3%A9s

            return id.ToByteArray().Select(b => (int)b).Sum() % partitionCount;

            //Min : 0
            //Max : 492
        }
    }
}

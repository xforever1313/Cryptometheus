//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Cryptometheus
{
    public class CryptonatorResult
    {
        // ---------------- Constructor ----------------

        public CryptonatorResult()
        {
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// The currency we want to get the price of.
        /// Set to null if <see cref="Success"/> is false.
        /// </summary>
        public string BaseCurrency { get; set; }

        /// <summary>
        /// The currency in which we want to display <see cref="BaseCurrency"/>'s price.
        /// Set to null if <see cref="Success"/> is false.
        /// </summary>
        public string TargetCurrency { get; set; }

        /// <summary>
        /// How much 1 <see cref="BaseCurrency"/> can be sold for in <see cref="TargetCurrency"/>
        /// Set to 0 if <see cref="Success"/> is false.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Was the query successful?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The error message.  Set to null if <see cref="Success"/> is true.
        /// </summary>
        public string Error { get; set; }
    }

    public static class CryptonatorResultExtensions
    {
        public static void FromJson( this CryptonatorResult result, string jsonString )
        {
            // Example success JSON:
            // {
            //     "ticker":
            //      {
            //          "base":"DOGE"
            //          "target":"USD",
            //          "price":"0.02788101",
            //          "volume":"3604478983.83879995",
            //          "change":"-0.00297482"
            //      },
            //      "timestamp":1612025582,
            //      "success":true,
            //      "error":""
            // }
            //
            // Example failure JSON:
            // {
            //     "success":false,
            //     "error":"Pair not found"
            // }

            // JObject json = JObject.Parse( jsonString );
            Console.WriteLine( jsonString );
        }
    }
}

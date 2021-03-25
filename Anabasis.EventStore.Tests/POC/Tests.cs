using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests.POC
{

  public class TestDocOne : IAnabasisDocument
  {
    public string Author { get; set; }
    public string Content { get; set; }
    public string Id { get; set; }
    public bool IsRootDocument => ParentId == null;
    public string ParentId { get; set; }
    public string Tag { get; set; }
    public string SomeExtraTwo { get; set; }
    public string SomeExtraOne { get; set; }
  }


  [TestFixture]
  public class Tests
  {

    [Test, Order(0)]
    public void ShouldRun()
    {

      IAnabasisDocument testDocOne = new TestDocOne()
      {
        Author = "Author",
        Tag = "Tag",
        Content = "Content",
        Id = "Id",
        ParentId = "Id",
        SomeExtraOne = "SomeExtraOne",
        SomeExtraTwo = "SomeExtraTwo",
      };

      var testDocOneSerialized = JsonConvert.SerializeObject(testDocOne);


    }
  }
}


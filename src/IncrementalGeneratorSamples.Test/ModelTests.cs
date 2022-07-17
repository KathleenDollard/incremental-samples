using IncrementalGeneratorSamples.InternalModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class ModelTests
    {



[Theory]
[InlineData("SimplestPractical", SimplestPractical)]
[InlineData("WithOneProperty", WithOneProperty)]
[InlineData("WithMulitipeProperties", WithMulitipeProperties)]
[InlineData("WithXmlDescripions", WithXmlDescripions)]
[InlineData("WithAttributeNamedValue", WithAttributeNamedValues)]
[InlineData("WithAttributeConstructorValues", WithAttributeConstructorValues)]
[InlineData("WithAttributeNestedNamedValues", WithAttributeNestedNamedValues)]
[InlineData("WithAttributeNestedConstructorValues", WithAttributeNestedConstructorValues)]
public Task Initial_class_model(string fileNamePart, InitialClassModel input)
{
    var (inputDiagnostics, output) = GetInitialClassModel(input, x => x.Identifier.ToString() == "MyClass");

    Assert.Empty(TestHelpers.ErrorAndWarnings(inputDiagnostics));
    return Verifier.Verify(output).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
}
    }

}
}

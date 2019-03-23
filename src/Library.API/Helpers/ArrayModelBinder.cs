using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
/**
 * Changed the var of the local variables to match their concrete names so that I could see what the type of the variables are.
 * */

namespace Library.API.Helpers
{
    public class ArrayModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (!bindingContext.ModelMetadata.IsEnumerableType) //If our model is not an enumeralble task.
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask; //Complete the task and exit
            }
            string value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).ToString(); //Get the inputed value from the provider. It should contain a string that are the list of guids.

            if (string.IsNullOrWhiteSpace(value))
            {
                bindingContext.Result = ModelBindingResult.Success(null); //Bad request and exit
                return Task.CompletedTask;
            }
            //Get the enumerable's type and a converter
            Type elementType = bindingContext.ModelType.GetTypeInfo().GenericTypeArguments[0]; //This generic type arguments should return guid.
            //Converters are built in and help, in this case, converting string types to guids. 
            //To get that converter we call GetConverter into the type decriptor class and pass it the element type which in our case its Guid. 
            TypeConverter converter = TypeDescriptor.GetConverter(elementType);
            //
            //Each string is convertred into guid from calling converter.ConvertFromString.
            object[] values = value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => converter.ConvertFromString(x.Trim())).ToArray();
            //Create an array with the Guid type, derived from the elementType variable, witht he specified lenght, which is derived from values.Length object array.
            Array typedValues = Array.CreateInstance(elementType, values.Length);
            //Copy the values of the values array into the typedValues array starting at destination index, in this case is 0
            values.CopyTo(typedValues, 0);
            //Set the typed values as our model on our binding context.
            bindingContext.Model = typedValues;
            //set result to succes and pass in the bindingContext model.
            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            return Task.CompletedTask;
        }
    }
}

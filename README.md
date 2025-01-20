## Getting started

* Install the NuGet package 'DavidHome.RssFeed.Optimizely'
* On a service collection, typically under your Startup.cs file or Program.cs, call ``AddDavidHomeRssFeed()`` with an instance of IConfiguration. This will return a RSS feed service builder. 
	* Use it to make a call onto ``AddOptimizelyFeedIntegration()``, still with an instance of IConfiguration
	* Call ``AddDefaultOptimizelyProcessors()`` to add the out of the box processors which automatically process your content data to something ingestable with the plugin when it builds your RSS feed. You can also omit its declaration and create your own processors. More on the subject later.
	* Call ``AddContentPageFeed<TContainer, TItem>()`` as many time as desired. This will flag the desired content types to be taken into account during the generation of RSS. 
* Install 'DavidHome.RssFeed.Storage.AzureBlob' to obtain blob container storage. 
	* Call ``AddAzureBlobStorage()`` to add its support integrated to the RSS feed mechanisms. An IConfiguration instance is required, which must contain the configuration for your storage account. Providing direct access to the connection string is enough in this situation. More details in the next sections. 

## Configuration

This plugin uses a configuration less approach, where the default should normally be enough. You can do personalize your settings by changing your appsettings.json or any other places where you have configured a custom configuration provider in your application. The structure is as followed: 
```json
{
  "DavidHome": {
    "RssFeed": {
      "SerializeExtensionsAsAtom": false,
      "ContentMaxLength": 25000000,
      "MyCustomContainerPageTypeName": {
        "ContentMaxLength": 15000000,
        "ContentAreaPropertyName": "MainContentArea",
        "FeedTitlePropertyName": "HeadTitle"
      },
      "AnotherPageType": {
        "ContentAreaPropertyName": "Content",
        "FeedTitlePropertyName": "Title"
      }
    }
  }
}
```
You can add as many child object as desired. You might have three different feeds pointing on three different page in your CMS tree and that is all supported by this plugin. You can personalize each feeds using this structure. The root level of the "RssFeed" node contains the default values for all content types.

The list of options are the following: 
| Name | Description | Default | 
|--|--|--|
| ContentMaxLength | Length of the content before the plugin trims it in the output of the RSS feed. | 1000000 |
| MaxSyndicationItems | Max number of syndication items that will be generated for a certain feed. | 50 |
| SerializeExtensionsAsAtom | When enabled, will serialize extensions within the Atom namespace. | false |

Additionnaly, the Optimizely specific options are the following: 
| Name | Description | Default | 
|--|--|--|
| FeedRelativeUrl | The relative URL of your RSS feed based on the location of your container page. | "rss" |
| ContentAreaPropertyName | The Optimizely content area having the content you want to display in your feed items. If given, the description field in the feed will be automatically populated with this property from your CMS. Otherwise no description tag is generated for all items. | null |
| FeedTitlePropertyName | The CMS field to use to pull the title from for your syndication link header tag. The Name property is used if not provided. | null |

### Azure Blob Configuration

For Optimizely DXP customers, simply use the following code snippet to load the existing connection string: 
```csharp
_configuration.GetSection("ConnectionStrings").GetSection("EPiServerAzureBlobs")
```
This needs to be provided when calling ``AddAzureBlobStorage()``. From there, you get the idea, you can provide any connection strings from any section of you configuration and it will work. Make sure to upgrade the package 'Microsoft.Extensions.Azure' to the latest, otherwise this will **not** work.

## Content Type Setup

You will have to customize your content types with marker interfaces:
* ``IRssFeedContainer<TFeedItem>`` on containers, where ``TFeedItem`` is your feed item content type. 
* ``IRssFeedItem<TFeedContainer>`` on items, where ``TFeedContainer`` is your container content type. 

**VERY IMPORTANT**: Certain properties are not supported by Optimizely and this is by design. Please add the ``IgnoreAttribute`` on top of them. 

Normally with both out of the box Optimizely processors, ``OptimizelyContentContainerProcessor`` and ``OptimizelyContentItemProcessor``, you can safely add the ``IgnoreAttribute`` on all properties added by the interfaces. 
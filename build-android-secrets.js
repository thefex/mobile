
var replacer = require("./node/secret-replace.js");

replacer.replaceSecrets("Phoebe/Build.cs", {
	// "string to replace" : "secret env variable"
	"{XAMARIN_INSIGHTS_API_KEY_ANDROID}" : "XAMARIN_INSIGHTS_API_KEY_ANDROID",
	"{GMC_SENDER_ID}" : "GCM_SENDER_ID",
	"{GOOGLE_ANALYTICS_ID}" : "GOOGLE_ANALYTICS_ID",
});
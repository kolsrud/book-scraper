# book-scraper

Basic tool for downloading the web page https://books.toscrape.com/ and making it locally accessible from disk.

## Usage:
Project produces binary BookScraper.exe that creates a folder called "LocalVersion" that will contain all files.
Access the file "LocalVersion/index.html" root level "index.html" in a browser to view local version.

## Limitations and future work:

* The tool is a proof of concept and has no configurable parts. The output folder and degree of parallelism (number of workers) are obvious candidates for being parameterized.
* Links are only retrieved from html files. In theory, there could be references to local files also from other sources (like javascript), but parsing or traversal of other formats than Html has been implemented in this project.
* No tests have been included in the project. The WorkerPool class and a number of the utility functions that were implemented would have benefited from this.
* It is assumed that all links have a relative path of the form where all

## Other potential future work:

There were a number of assumptions and design choices I would probably have revisited as part of an overall review of the solution. Among these are:
* The WorkerPool concept: I already had an implementation of this class in code for an earlier project so it was easily accessible to me. However, I would probably have investigated further the built in parallelism constraints of the HttpClient class if I were to spend more time on this project. The main goal of the class is to control the number of concurrent downloads so as to prevent the tool from creating an unacceptable large number of concurrent requests.
* The tool seems to be visiting a lot of nodes multiple time. That is not really a problem as the work items revisiting a node exit immediately, but there is room for improvement here.
* Files are downloaded from the Internet using two different technologies, HttpClient and HtmlWeb. I would have preferred unifying these.

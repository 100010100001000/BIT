# Binary Interpolation Tree (B.I.T)

Binary Interpolation Tree is a machine learning model i have been working on mostly out of curiosity. Please let me know if my terminology is off, i made any other errors, or something very similar already exists. 

The main idea was to conceptionalize a model that provides:

* fast runtime on cpu's in relation to model complexity (O(n), Ω(1), input dependent)
* the ability to further simplify computations based on a maximum inaccuracy
* simple and fast training
* very low initial model complexity that increases during training
* multithreading capabilities for maximum cpu usage
* increased transparency compared to other ML models
* a parameter regarding determinism of the algorithm

[This](https://drive.google.com/file/d/19SjIzYrD3dIlTMvCeS17ep8FtpKrsS5n/view?usp=sharing) is a link to a demo of this algorithm for windows only. You can raise the parameter "draw resolution" to see the model in greater detail, or lower it to get a faster runtime. You don't have to understand or change the other parameters to use this demo, but if you want to, refer to the pdf.

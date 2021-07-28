# Binary Interpolation Tree (B.I.T)

Binary Interpolation Tree is a machine learning model i am working on. I am by no means an expert in this field or computer science in general and have been working on this mostly out of curiosity. Please let me know if my terminology is off, i made any other errors, or something very similar already exists. 

The main idea was to conceptionalize a model that provides:

* fast runtime on cpu's in relation to model complexity (O(n), Ω(1), input dependent)
* the ability to further simplify computations based on a maximum inaccuracy.
* simple and fast training
* very low initial model complexity that increases during training
* multithreading capabilitys for maximum cpu usage
* increased transparency compared to other ML models
* a parameter regarding determinism of the algorithm

As the name sugests, this model is basicly a binary tree. This binary tree consists of nodes and end-nodes. Nodes alwas have 2 children and end-nodes have non. Every node also has a reference to its parent, except for the root-node wich has no parent. For the purpose of describing this system mathematically, every node ![equation](https://latex.codecogs.com/gif.latex?n) has the following variables associated with it:

![equation](https://latex.codecogs.com/gif.latex?id%28n%29) a unique identifier
![equation](https://latex.codecogs.com/gif.latex?c_0%28id%28n%29%29) the id of the first child
![equation](https://latex.codecogs.com/gif.latex?c_1%28id%28n%29%29) the id of the second child

To calculate the output of this model for a given input, this binary tree is traversed recursivly, from the root-node, to the end-nodes. In this process every endnode will just assign its output vector to ![equation](https://latex.codecogs.com/gif.latex?%5Cvec%7Br_i%7D) where ![equation](https://latex.codecogs.com/gif.latex?i) is the unique identifier of this node. When both children have been evaluated and using the position of the child = ![equation](https://latex.codecogs.com/gif.latex?%5Cvec%7Bp_i%7D), the parent will calculate the follwing:

the following statements are true about InterpolationFactor(Vector p1, Vector p2, Vector arg) = I(p1, p2, x):

![equation](https://latex.codecogs.com/gif.latex?I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bp_1%7D%29%20%3D%200)  
![equation](https://latex.codecogs.com/gif.latex?I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bp_2%7D%29%20%3D%201)  
![equation](https://latex.codecogs.com/gif.latex?%28%5Cvec%7Bp_2%7D%20-%20%5Cvec%7Bp_1%7D%29%20%5Ccirc%20%28%5Cvec%7Bx_1%7D%20-%20%5Cvec%7Bx_2%7D%29%3D0%20%5CLeftrightarrow%20I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bx_1%7D%29%20%3D%20I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bx_2%7D%29)
